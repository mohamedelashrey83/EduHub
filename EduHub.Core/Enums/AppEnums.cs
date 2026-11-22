namespace EduHub.Core.Enums;

public enum CourseLevel
{
    AllLevels = 0,
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3
}

public enum CourseStatus
{
    Draft = 0,
    PendingApproval = 1,
    Published = 2,
    Rejected = 3,
    Archived = 4
}

public enum QuestionType
{
    MultipleChoice = 0,
    TrueFalse = 1,
    FillInBlank = 2
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Instructor = "Instructor";
    public const string Student = "Student";
}
