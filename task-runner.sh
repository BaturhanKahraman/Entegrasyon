#!/bin/bash
#
# Task Runner for Entegrasyon Project
# Automatically processes tasks.json with Claude Code, handling session limits and checkpoints
#
# Usage:
#   ./task-runner.sh [tasks.json]     # Start fresh
#   ./task-runner.sh --resume         # Resume from checkpoint
#   ./task-runner.sh --status         # Show current status
#

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEFAULT_TASKS_FILE="${SCRIPT_DIR}/tasks.json"
STATE_FILE="${SCRIPT_DIR}/.task_runner_state.json"
CLAUDE_BIN="claude"
SESSION_LIMIT_SECONDS=$((5 * 60 * 60))  # 5 hours
SESSION_WARNING_SECONDS=$((4 * 60 * 60 + 30 * 60))  # 4.5 hours (warning)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Initialize state file
init_state() {
    local tasks_file="${1:-$DEFAULT_TASKS_FILE}"

    if [[ ! -f "$tasks_file" ]]; then
        log_error "Tasks file not found: $tasks_file"
        exit 1
    fi

    # Validate JSON
    if ! jq empty "$tasks_file" 2>/dev/null; then
        log_error "Invalid JSON in tasks file: $tasks_file"
        exit 1
    fi

    cat > "$STATE_FILE" << EOF
{
    "tasks_file": "$tasks_file",
    "current_task_index": 0,
    "current_phase": "plan",
    "session_start": $(date +%s),
    "completed_tasks": [],
    "failed_tasks": [],
    "notes": {}
}
EOF
    log_info "Initialized new state file: $STATE_FILE"
}

# Load state
load_state() {
    if [[ ! -f "$STATE_FILE" ]]; then
        log_error "No state file found. Run without --resume first."
        exit 1
    fi

    STATE_TASKS_FILE=$(jq -r '.tasks_file' "$STATE_FILE")
    STATE_CURRENT_INDEX=$(jq -r '.current_task_index' "$STATE_FILE")
    STATE_PHASE=$(jq -r '.current_phase' "$STATE_FILE")
    STATE_SESSION_START=$(jq -r '.session_start' "$STATE_FILE")
}

# Save state
save_state() {
    local index="${1:-$STATE_CURRENT_INDEX}"
    local phase="${2:-$STATE_PHASE}"
    local session_start="${3:-$STATE_SESSION_START}"

    jq --arg idx "$index" \
       --arg phase "$phase" \
       --arg session "$session_start" \
       '.current_task_index = ($idx | tonumber) |
        .current_phase = $phase |
        .session_start = ($session | tonumber)' \
       "$STATE_FILE" > "${STATE_FILE}.tmp" && mv "${STATE_FILE}.tmp" "$STATE_FILE"
}

# Update task notes
update_task_notes() {
    local task_idx="$1"
    local key="$2"
    local value="$3"

    jq --arg idx "$task_idx" \
       --arg key "$key" \
       --arg val "$value" \
       '.notes[$idx][$key] = $val' \
       "$STATE_FILE" > "${STATE_FILE}.tmp" && mv "${STATE_FILE}.tmp" "$STATE_FILE"
}

# Mark task as completed
mark_task_completed() {
    local task_idx="$1"
    local task_name="$2"

    jq --arg idx "$task_idx" \
       --arg name "$task_name" \
       '.completed_tasks += [{"index": ($idx | tonumber), "name": $name, "completed_at": now}]' \
       "$STATE_FILE" > "${STATE_FILE}.tmp" && mv "${STATE_FILE}.tmp" "$STATE_FILE"
}

# Mark task as failed
mark_task_failed() {
    local task_idx="$1"
    local task_name="$2"
    local error_msg="$3"

    jq --arg idx "$task_idx" \
       --arg name "$task_name" \
       --arg error "$error_msg" \
       '.failed_tasks += [{"index": ($idx | tonumber), "name": $name, "error": $error, "failed_at": now}]' \
       "$STATE_FILE" > "${STATE_FILE}.tmp" && mv "${STATE_FILE}.tmp" "$STATE_FILE"
}

# Check session time
check_session_time() {
    local current_time=$(date +%s)
    local elapsed=$((current_time - STATE_SESSION_START))
    local remaining=$((SESSION_LIMIT_SECONDS - elapsed))

    if [[ $remaining -le 0 ]]; then
        log_warn "Session limit (5 hours) reached!"
        return 1
    elif [[ $remaining -le 1800 ]]; then  # 30 minutes remaining
        log_warn "Session expires in ~$((remaining / 60)) minutes"
        return 2
    fi

    return 0
}

# Get tasks sorted by priority
get_sorted_tasks() {
    # Priority order: critical > high > medium > low
    jq 'to_entries |
        sort_by(.value.priority as $p |
            if $p == "critical" then 0
            elif $p == "high" then 1
            elif $p == "medium" then 2
            elif $p == "low" then 3
            else 4 end) |
        map(.value + {original_index: .key | tonumber})' \
        "$STATE_TASKS_FILE"
}

# Get specific task by index
get_task() {
    local idx="$1"
    jq --arg idx "$idx" '.[$idx | tonumber]' "$STATE_TASKS_FILE"
}

# Run plan phase
run_plan_phase() {
    local task="$1"
    local task_idx="$2"

    local task_name=$(echo "$task" | jq -r '.task')
    local description=$(echo "$task" | jq -r '.description')

    log_info "=== PLAN PHASE: $task_name ==="
    log_info "Description: $description"
    echo ""

    # Generate plan using Claude in non-interactive mode
    log_info "Generating plan with Claude..."

    local plan_output
    plan_output=$($CLAUDE_BIN -p \
        --output-format json \
        --no-session-persistence \
        "You are a software architect. Create a detailed implementation plan for this task.

Task: $task_name
Description: $description

Provide:
1. High-level approach
2. Files to modify/create
3. Step-by-step implementation steps
4. Testing strategy
5. Potential risks and mitigations

Output as JSON with fields: summary, files, steps, testing, risks" 2>/dev/null || echo "")

    if [[ -z "$plan_output" ]]; then
        log_error "Failed to generate plan"
        return 1
    fi

    # Extract plan (handle both JSON and text output)
    local plan_text
    if echo "$plan_output" | jq empty 2>/dev/null; then
        plan_text=$(echo "$plan_output" | jq -r '.summary // .text // . // empty')
    else
        plan_text="$plan_output"
    fi

    echo ""
    echo "=========================================="
    echo "GENERATED PLAN:"
    echo "=========================================="
    echo "$plan_text"
    echo "=========================================="
    echo ""

    # Save plan to notes
    update_task_notes "$task_idx" "plan" "$plan_text"

    # Wait for user approval
    while true; do
        read -p "Do you approve this plan and want to proceed with implementation? (yes/no/edit): " answer
        case $answer in
            [Yy][Ee][Ss])
                log_success "Plan approved! Proceeding with implementation..."
                return 0
                ;;
            [Nn][Oo])
                log_warn "Plan rejected. Skipping this task..."
                return 1
                ;;
            [Ee][Dd][Ii][Tt])
                read -p "Enter your feedback/edits for the plan: " feedback
                update_task_notes "$task_idx" "plan_feedback" "$feedback"
                log_info "Regenerating plan with feedback..."
                # Could loop back to regenerate, for now just continue
                log_warn "Manual edit mode - you'll need to implement this task yourself"
                return 1
                ;;
            *)
                echo "Please answer yes, no, or edit"
                ;;
        esac
    done
}

# Run implementation phase
run_implement_phase() {
    local task="$1"
    local task_idx="$2"

    local task_name=$(echo "$task" | jq -r '.task')
    local description=$(echo "$task" | jq -r '.description')
    local plan=$(jq -r --arg idx "$task_idx" '.notes[$idx].plan // empty' "$STATE_FILE")

    log_info "=== IMPLEMENTATION PHASE: $task_name ==="

    # Build prompt
    local prompt="Implement the following task:

Task: $task_name
Description: $description
"

    if [[ -n "$plan" && "$plan" != "null" ]]; then
        prompt+="
Implementation Plan:
$plan

Follow the plan above and implement the task."
    fi

    # Add unit test requirement if specified
    local add_unit_tests=$(echo "$task" | jq -r '.addOrUpdateUnitTests // false')
    if [[ "$add_unit_tests" == "true" ]]; then
        prompt+="

IMPORTANT: Include unit tests following the TDD-First approach from CLAUDE.md:
1. Write or update the test first
2. Run the test to verify it fails
3. Implement the minimum code to make it pass
4. Run the test again to verify it passes
5. Refactor if needed"
    fi

    # Add integration test requirement if specified
    local add_integration_tests=$(echo "$task" | jq -r '.addOrUpdateIntegrationTests // false')
    if [[ "$add_integration_tests" == "true" ]]; then
        prompt+="

Also add or update integration tests using Testcontainers as specified in CLAUDE.md."
    fi

    # Run implementation
    log_info "Starting implementation with Claude..."
    if ! $CLAUDE_BIN -p "$prompt"; then
        log_error "Implementation failed"
        return 1
    fi

    log_success "Implementation completed!"
    return 0
}

# Run manual test steps
run_manual_tests() {
    local task="$1"
    local task_idx="$2"

    local task_name=$(echo "$task" | jq -r '.task')
    local test_steps=$(echo "$task" | jq -r '.manual_test_steps')

    # Check if manual_test_steps exists and is not empty
    if [[ "$test_steps" == "null" || "$test_steps" == "[]" ]]; then
        return 0
    fi

    log_info "=== MANUAL TEST STEPS: $task_name ==="
    echo ""

    local step_count=$(echo "$test_steps" | jq 'length')
    log_info "Found $step_count manual test step(s)"

    for i in $(seq 0 $((step_count - 1))); do
        local step=$(echo "$test_steps" | jq -r ".[$i]")
        echo ""
        echo "Step $((i + 1))/$step_count: $step"
        echo "----------------------------------------"

        while true; do
            read -p "Did this step pass? (yes/no/skip): " result
            case $result in
                [Yy][Ee][Ss])
                    log_success "Step $((i + 1)) passed"
                    update_task_notes "$task_idx" "test_step_$i" "passed"
                    break
                    ;;
                [Nn][Oo])
                    log_error "Step $((i + 1)) failed"
                    update_task_notes "$task_idx" "test_step_$i" "failed"

                    read -p "Continue to next step or abort task? (continue/abort): " action
                    if [[ "$action" == "abort" ]]; then
                        return 1
                    fi
                    break
                    ;;
                [Ss][Kk][Ii][Pp])
                    log_warn "Step $((i + 1)) skipped"
                    update_task_notes "$task_idx" "test_step_$i" "skipped"
                    break
                    ;;
                *)
                    echo "Please answer yes, no, or skip"
                    ;;
            esac
        done
    done

    log_success "All manual test steps processed!"
    return 0
}

# Run a single task
run_task() {
    local task_idx="$1"
    local task
    task=$(get_task "$task_idx")

    local task_name=$(echo "$task" | jq -r '.task')
    local needs_plan=$(echo "$task" | jq -r '.plan // false')

    log_info "Starting task $((task_idx + 1)): $task_name"

    # Check session time before starting
    check_session_time
    local time_status=$?

    if [[ $time_status -eq 1 ]]; then
        # Session expired
        log_warn "Session expired. Restarting with new session..."
        exec "$0" --resume
    fi

    # Plan phase (if needed)
    if [[ "$needs_plan" == "true" && "$STATE_PHASE" == "plan" ]]; then
        if ! run_plan_phase "$task" "$task_idx"; then
            log_warn "Plan phase rejected or failed. Skipping implementation."
            mark_task_failed "$task_idx" "$task_name" "Plan rejected by user"
            return 1
        fi
        STATE_PHASE="implement"
        save_state "$task_idx" "$STATE_PHASE"
    fi

    # Implementation phase
    if [[ "$STATE_PHASE" == "implement" ]]; then
        if ! run_implement_phase "$task" "$task_idx"; then
            log_error "Implementation failed for task: $task_name"
            mark_task_failed "$task_idx" "$task_name" "Implementation failed"
            return 1
        fi
        STATE_PHASE="test"
        save_state "$task_idx" "$STATE_PHASE"
    fi

    # Test phase
    if [[ "$STATE_PHASE" == "test" ]]; then
        if ! run_manual_tests "$task" "$task_idx"; then
            log_warn "Manual tests had issues for task: $task_name"
            # Don't fail, just warn - implementation might still be valid
        fi
        STATE_PHASE="complete"
        save_state "$task_idx" "$STATE_PHASE"
    fi

    # Mark complete
    mark_task_completed "$task_idx" "$task_name"
    STATE_PHASE="plan"  # Reset for next task
    save_state "$((task_idx + 1))" "$STATE_PHASE"

    log_success "Task completed: $task_name"
    echo ""
    echo "=========================================="
    echo ""

    return 0
}

# Show status
show_status() {
    if [[ ! -f "$STATE_FILE" ]]; then
        log_info "No active task runner session found."
        exit 0
    fi

    load_state

    local total_tasks=$(jq 'length' "$STATE_TASKS_FILE")
    local completed=$(jq '.completed_tasks | length' "$STATE_FILE")
    local failed=$(jq '.failed_tasks | length' "$STATE_FILE")
    local current_time=$(date +%s)
    local elapsed=$((current_time - STATE_SESSION_START))
    local remaining=$((SESSION_LIMIT_SECONDS - elapsed))

    echo "=========================================="
    echo "Task Runner Status"
    echo "=========================================="
    echo "Tasks file: $STATE_TASKS_FILE"
    echo "Progress: $completed/$total_tasks completed"
    echo "Failed: $failed"
    echo "Current task index: $STATE_CURRENT_INDEX"
    echo "Current phase: $STATE_PHASE"
    echo "Session started: $(date -d @$STATE_SESSION_START '+%Y-%m-%d %H:%M:%S')"
    echo "Session remaining: $((remaining / 3600))h $(((remaining % 3600) / 60))m"
    echo "=========================================="

    if [[ $completed -gt 0 ]]; then
        echo ""
        echo "Completed tasks:"
        jq -r '.completed_tasks[] | "  ✓ [\(.index)] \(.name) (\(.completed_at))"' "$STATE_FILE"
    fi

    if [[ $failed -gt 0 ]]; then
        echo ""
        echo "Failed tasks:"
        jq -r '.failed_tasks[] | "  ✗ [\(.index)] \(.name): \(.error)"' "$STATE_FILE"
    fi
}

# Main function
main() {
    case "${1:-}" in
        --resume)
            log_info "Resuming from checkpoint..."
            load_state
            ;;
        --status)
            show_status
            exit 0
            ;;
        --reset)
            log_warn "Resetting state file..."
            rm -f "$STATE_FILE"
            log_success "State file reset."
            exit 0
            ;;
        --help|-h)
            echo "Task Runner for Entegrasyon Project"
            echo ""
            echo "Usage:"
            echo "  $0 [tasks.json]       Start with tasks file (default: tasks.json)"
            echo "  $0 --resume           Resume from checkpoint"
            echo "  $0 --status           Show current status"
            echo "  $0 --reset            Reset state file"
            echo "  $0 --help             Show this help"
            echo ""
            echo "Task JSON format:"
            echo '  [{"task": "Name", "description": "...", "priority": "critical|high|medium|low",'
            echo '    "plan": true|false, "manual_test_steps": [...],'
            echo '    "addOrUpdateUnitTests": true|false, "addOrUpdateIntegrationTests": true|false}]'
            exit 0
            ;;
        "")
            init_state "$DEFAULT_TASKS_FILE"
            ;;
        *)
            if [[ -f "$1" ]]; then
                init_state "$1"
            else
                log_error "Unknown option or file not found: $1"
                echo "Use --help for usage information"
                exit 1
            fi
            ;;
    esac

    load_state

    local total_tasks
    total_tasks=$(jq 'length' "$STATE_TASKS_FILE")

    log_info "Task Runner started"
    log_info "Total tasks: $total_tasks"
    log_info "Starting from task index: $STATE_CURRENT_INDEX"
    echo ""

    while [[ $STATE_CURRENT_INDEX -lt $total_tasks ]]; do
        if ! run_task "$STATE_CURRENT_INDEX"; then
            log_warn "Task $STATE_CURRENT_INDEX did not complete successfully"
            # Continue to next task anyway
            STATE_CURRENT_INDEX=$((STATE_CURRENT_INDEX + 1))
            STATE_PHASE="plan"
            save_state "$STATE_CURRENT_INDEX" "$STATE_PHASE"
        fi
    done

    log_success "All tasks processed!"
    echo ""
    show_status

    # Cleanup
    rm -f "$STATE_FILE"
    log_info "State file cleaned up."
}

# Check dependencies
check_dependencies() {
    if ! command -v jq &> /dev/null; then
        log_error "jq is required but not installed. Install with: sudo dnf install jq"
        exit 1
    fi

    if ! command -v "$CLAUDE_BIN" &> /dev/null; then
        log_error "claude command not found in PATH"
        exit 1
    fi
}

# Run
check_dependencies
main "$@"
