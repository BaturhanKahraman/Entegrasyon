using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Entity.Dtos.Storefront;

public record LoyaltyDashboardDto(
    int ActiveMembers,
    int TotalEarnedPoints,
    int TotalSpentPoints,
    int ThisMonthEarned,
    List<LoyaltyMemberDto> TopMembers);

public record LoyaltyMemberDto(
    int CustomerId,
    string? CustomerName,
    int TotalEarned,
    int CurrentBalance,
    string Level);
