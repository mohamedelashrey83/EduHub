namespace EduHub.Core.Entities;

public class Review
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public virtual Course? Course { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public virtual ApplicationUser? Student { get; set; }

    public int Rating { get; set; } // 1 - 5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int HelpfulVotesCount { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<ReviewHelpfulVote> HelpfulVotes { get; set; } = new List<ReviewHelpfulVote>();
}

public class ReviewHelpfulVote
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public virtual Review? Review { get; set; }

    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser? User { get; set; }

    public bool IsHelpful { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
