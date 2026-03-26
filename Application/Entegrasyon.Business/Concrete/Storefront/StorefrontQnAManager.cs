using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontQnAManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontQnAManager
{
    public async Task<IDataResult<List<StorefrontProductQuestion>>> GetProductQuestionsAsync(int tenantId, Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var questions = await dbContext.StorefrontProductQuestions
            .Where(q => q.TenantId == tenantId && q.ProductId == productId && q.IsPublished)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontProductQuestion>>(questions);
    }

    public async Task<IResult> AskQuestionAsync(int tenantId, Guid productId, int customerId, string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new ErrorResult("Soru bos olamaz.");

        if (question.Length > 1000)
            return new ErrorResult("Soru en fazla 1000 karakter olabilir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var entity = new StorefrontProductQuestion
        {
            TenantId = tenantId,
            ProductId = productId,
            CustomerId = customerId,
            QuestionText = question.Trim(),
            IsPublished = true // auto-published
        };

        dbContext.StorefrontProductQuestions.Add(entity);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Sorunuz basariyla gonderildi.");
    }

    public async Task<IResult> AnswerQuestionAsync(int questionId, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return new ErrorResult("Cevap bos olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var question = await dbContext.StorefrontProductQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question is null)
            return new ErrorResult("Soru bulunamadi.");

        question.AnswerText = answer.Trim();
        question.AnsweredAt = DateTimeOffset.UtcNow;
        question.IsPublished = true;
        dbContext.StorefrontProductQuestions.Update(question);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Cevap basariyla kaydedildi.");
    }

    public async Task<IDataResult<List<StorefrontProductQuestion>>> GetUnansweredQuestionsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var questions = await dbContext.StorefrontProductQuestions
            .Where(q => q.TenantId == tenantId && q.AnswerText == null)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontProductQuestion>>(questions);
    }

    public async Task<IResult> MarkHelpfulAsync(int questionId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var question = await dbContext.StorefrontProductQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question is null)
            return new ErrorResult("Soru bulunamadi.");

        question.HelpfulCount++;
        dbContext.StorefrontProductQuestions.Update(question);
        await dbContext.SaveChangesAsync();

        return new SuccessResult();
    }
}
