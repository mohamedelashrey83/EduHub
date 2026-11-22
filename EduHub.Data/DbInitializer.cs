using EduHub.Core.Entities;
using EduHub.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(EduHubDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Apply pending migrations or ensure created
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roles = { UserRoles.Admin, UserRoles.Instructor, UserRoles.Student };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Users
        ApplicationUser? adminUser = await userManager.FindByEmailAsync("admin@eduhub.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@eduhub.com",
                Email = "admin@eduhub.com",
                FullName = "System Administrator",
                Headline = "EduHub Platform Operations",
                Bio = "Platform oversight, course quality assurance, and system governance.",
                EmailConfirmed = true,
                AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150"
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            }
        }

        ApplicationUser? instructorUser = await userManager.FindByEmailAsync("instructor@eduhub.com");
        if (instructorUser == null)
        {
            instructorUser = new ApplicationUser
            {
                UserName = "instructor@eduhub.com",
                Email = "instructor@eduhub.com",
                FullName = "Dr. Alexander Wright",
                Headline = "Principal Cloud Architect & Author",
                Bio = "Former Microsoft engineer with 15+ years architecting enterprise distributed software, microservices, and high-performance cloud applications.",
                WebsiteUrl = "https://alexanderwright.dev",
                LinkedInUrl = "https://linkedin.com/in/alexanderwright",
                EmailConfirmed = true,
                TotalEarnings = 2840.50m,
                AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150"
            };
            var result = await userManager.CreateAsync(instructorUser, "Instructor@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(instructorUser, UserRoles.Instructor);
            }
        }

        ApplicationUser? studentUser = await userManager.FindByEmailAsync("student@eduhub.com");
        if (studentUser == null)
        {
            studentUser = new ApplicationUser
            {
                UserName = "student@eduhub.com",
                Email = "student@eduhub.com",
                FullName = "Sarah Jenkins",
                Headline = "Aspiring Full-Stack Software Engineer",
                Bio = "Lifelong learner enthusiastic about ASP.NET Core, cloud architectures, and modern UI engineering.",
                EmailConfirmed = true,
                AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150"
            };
            var result = await userManager.CreateAsync(studentUser, "Student@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, UserRoles.Student);
            }
        }

        // 3. Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            var webDev = new Category
            {
                Name = "Web Development",
                Slug = "web-development",
                Description = "Master front-end and back-end development with contemporary frameworks and architectures.",
                IconClass = "bi-code-slash"
            };

            var cloud = new Category
            {
                Name = "Cloud & DevOps",
                Slug = "cloud-devops",
                Description = "Scale resilient infrastructure using Microsoft Azure, Docker, Kubernetes, and CI/CD pipelines.",
                IconClass = "bi-cloud-check"
            };

            var softwareArch = new Category
            {
                Name = "Software Architecture",
                Slug = "software-architecture",
                Description = "Clean architecture, enterprise patterns, domain-driven design, and system scalability.",
                IconClass = "bi-diagram-3"
            };

            var dataAi = new Category
            {
                Name = "Data Science & AI",
                Slug = "data-ai",
                Description = "Predictive modeling, deep learning, PyTorch, and generative AI integration.",
                IconClass = "bi-cpu"
            };

            await context.Categories.AddRangeAsync(webDev, cloud, softwareArch, dataAi);
            await context.SaveChangesAsync();

            // Subcategories
            var aspnet = new Category { Name = "ASP.NET Core", Slug = "aspnet-core", ParentCategoryId = webDev.Id, IconClass = "bi-filetype-cs" };
            var azureSub = new Category { Name = "Azure Solutions", Slug = "azure-solutions", ParentCategoryId = cloud.Id, IconClass = "bi-cloud" };
            var patternsSub = new Category { Name = "Design Patterns", Slug = "design-patterns", ParentCategoryId = softwareArch.Id, IconClass = "bi-boxes" };

            await context.Categories.AddRangeAsync(aspnet, azureSub, patternsSub);
            await context.SaveChangesAsync();
        }

        // 4. Seed Courses
        if (!await context.Courses.AnyAsync())
        {
            var webDevCategory = await context.Categories.FirstAsync(c => c.Slug == "web-development");
            var cloudCategory = await context.Categories.FirstAsync(c => c.Slug == "cloud-devops");
            var archCategory = await context.Categories.FirstAsync(c => c.Slug == "software-architecture");

            // Course 1: ASP.NET Core Masterclass
            var course1 = new Course
            {
                Title = "Enterprise ASP.NET Core & Clean Architecture",
                Subtitle = "Build scalable, production-grade applications using N-Tier, CQRS, EF Core, and Azure",
                Description = @"<p>In this comprehensive masterclass, you will journey from foundational enterprise concepts to shipping high-availability web applications built with <strong>ASP.NET Core MVC</strong>, <strong>Entity Framework Core</strong>, and <strong>Microsoft Azure</strong>.</p>
                                <p>We cover clean N-Tier architecture, Unit of Work, Repository Pattern, Stripe payment processing, real-time video streaming, and automated PDF certification.</p>",
                Requirements = "• Basic knowledge of C# and object-oriented programming.\n• Visual Studio 2022 or VS Code.\n• Willingness to build real-world enterprise projects.",
                WhatYouWillLearn = "• Master N-Tier and Clean Architecture in .NET 10\n• Implement Repository & Unit of Work patterns efficiently\n• Integrate Stripe payments and Azure Blob Storage\n• Build interactive video classrooms with Video.js\n• Generate verifiable PDF certificates upon graduation",
                Price = 84.99m,
                DiscountPrice = 49.99m,
                Level = CourseLevel.Intermediate,
                Status = CourseStatus.Published,
                ThumbnailUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=600",
                PromoVideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
                Language = "English",
                InstructorId = instructorUser!.Id,
                CategoryId = webDevCategory.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };

            // Section 1
            var sec1 = new CourseSection { Title = "Module 1: Foundations & Architecture Design", OrderIndex = 1 };
            sec1.Lessons.Add(new Lesson
            {
                Title = "Course Overview & Architectural Philosophy",
                Description = "Welcome to EduHub! Discover the blueprint of enterprise systems and the layers of our application.",
                VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
                DurationSeconds = 345,
                OrderIndex = 1,
                IsFreePreview = true,
                ContentNotes = "Key takeaway: Separation of concerns guarantees maintainability as the team grows."
            });
            sec1.Lessons.Add(new Lesson
            {
                Title = "Setting Up N-Tier Architecture in ASP.NET Core",
                Description = "Step-by-step breakdown of EduHub.Core, EduHub.Data, EduHub.Services, and EduHub.Web.",
                VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4",
                DurationSeconds = 512,
                OrderIndex = 2,
                IsFreePreview = true,
                ContentNotes = "Always keep domain entities POCO-first without heavy infrastructure dependencies."
            });
            sec1.Lessons.Add(new Lesson
            {
                Title = "Repository Pattern & Unit of Work in Action",
                Description = "How to abstract database persistence and handle multiple operations in a unified transaction.",
                VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerFun.mp4",
                DurationSeconds = 620,
                OrderIndex = 3,
                IsFreePreview = false,
                ContentNotes = "Unit of Work coordinates multiple repositories under a single DbContext transaction."
            });

            // Module 1 Quiz
            var quiz1 = new Quiz
            {
                Title = "Module 1 Checkpoint: Architecture & Design Patterns",
                Description = "Test your grasp on enterprise design patterns, transaction boundaries, and layer separation.",
                PassingScorePercentage = 70
            };
            var q1 = new QuizQuestion
            {
                QuestionText = "Which layer in N-Tier architecture is responsible for direct user interaction and controllers?",
                QuestionType = QuestionType.MultipleChoice,
                Explanation = "The Presentation Layer (EduHub.Web) handles HTTP requests, views, and user interactions.",
                Points = 1,
                OrderIndex = 1
            };
            q1.Answers.Add(new QuizAnswer { AnswerText = "EduHub.Web (Presentation Layer)", IsCorrect = true });
            q1.Answers.Add(new QuizAnswer { AnswerText = "EduHub.Data (Data Access Layer)", IsCorrect = false });
            q1.Answers.Add(new QuizAnswer { AnswerText = "EduHub.Services (Business Logic Layer)", IsCorrect = false });
            quiz1.Questions.Add(q1);

            var q2 = new QuizQuestion
            {
                QuestionText = "The Unit of Work pattern ensures that all repository updates commit in a single database transaction.",
                QuestionType = QuestionType.TrueFalse,
                Explanation = "True! Unit of Work coordinates multiple repositories under one atomic transaction commit.",
                Points = 1,
                OrderIndex = 2
            };
            q2.Answers.Add(new QuizAnswer { AnswerText = "True", IsCorrect = true });
            q2.Answers.Add(new QuizAnswer { AnswerText = "False", IsCorrect = false });
            quiz1.Questions.Add(q2);

            sec1.Quizzes.Add(quiz1);
            course1.Sections.Add(sec1);

            // Section 2
            var sec2 = new CourseSection { Title = "Module 2: Advanced Business Logic & Design Patterns", OrderIndex = 2 };
            sec2.Lessons.Add(new Lesson
            {
                Title = "Strategy Pattern for Pricing & Dynamic Discounts",
                Description = "Implementing Strategy Pattern to evaluate coupons, holiday sales, and volume bundles.",
                VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyBlazes.mp4",
                DurationSeconds = 715,
                OrderIndex = 1,
                IsFreePreview = false,
                ContentNotes = "Strategy allows swapping pricing calculation algorithms at runtime cleanly."
            });
            sec2.Lessons.Add(new Lesson
            {
                Title = "Observer Pattern: Tracking Progress & Auto Certificates",
                Description = "Subscribing observers to course completion events for automated certification and student notifications.",
                VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerMeltdowns.mp4",
                DurationSeconds = 580,
                OrderIndex = 2,
                IsFreePreview = false,
                ContentNotes = "Decouples the progress tracking engine from notification channels and PDF generation."
            });
            course1.Sections.Add(sec2);

            // Course 2: Azure Cloud Solutions Architect
            var course2 = new Course
            {
                Title = "Microsoft Azure Solutions Architect Certification",
                Subtitle = "Master Azure Blob Storage, CDN, App Services, Key Vault, and Resilient Architecture",
                Description = @"<p>Prepare for senior cloud roles by architecting end-to-end media and e-learning platforms on Azure.</p>
                                <p>Includes signed SAS tokens for video streaming, Azure Blob storage tiering, performance caching, and automated deployment.</p>",
                Requirements = "• Familiarity with cloud basics and C#.\n• Free or paid Azure subscription.",
                WhatYouWillLearn = "• Azure App Service deployment\n• Blob storage containers, SAS signatures & CDN\n• High performance caching and disaster recovery",
                Price = 99.99m,
                DiscountPrice = 64.99m,
                Level = CourseLevel.Advanced,
                Status = CourseStatus.Published,
                ThumbnailUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=600",
                PromoVideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/TearsOfSteel.mp4",
                Language = "English",
                InstructorId = instructorUser!.Id,
                CategoryId = cloudCategory.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            };
            var sec2_1 = new CourseSection { Title = "Module 1: Azure Storage & CDN Delivery", OrderIndex = 1 };
            sec2_1.Lessons.Add(new Lesson
            {
                Title = "Azure Blob Storage Hierarchy & SAS Tokens",
                Description = "Generating time-limited signed URLs to protect video streams from unauthorized downloads.",
                VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/WeAreGoingOnBullrun.mp4",
                DurationSeconds = 480,
                OrderIndex = 1,
                IsFreePreview = true
            });
            course2.Sections.Add(sec2_1);

            // Course 3: Free Course for Immediate Testing
            var course3 = new Course
            {
                Title = "C# Design Patterns Quickstart (Free Edition)",
                Subtitle = "Learn Strategy, Observer, Repository and Dependency Injection with hands-on code",
                Description = @"<p>A fast-paced, 100% free course designed to teach you four mission-critical Gang of Four design patterns used throughout modern .NET software engineering.</p>",
                Requirements = "• No prerequisites. Jump right in!",
                WhatYouWillLearn = "• Strategy Pattern for interchangeability\n• Observer Pattern for reactive event notification\n• Repository & Unit of Work for database abstraction",
                Price = 0.00m,
                DiscountPrice = 0.00m,
                Level = CourseLevel.Beginner,
                Status = CourseStatus.Published,
                ThumbnailUrl = "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=600",
                PromoVideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/WhatCarCanYouGetForAGrand.mp4",
                Language = "English",
                InstructorId = instructorUser!.Id,
                CategoryId = archCategory.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            var sec3_1 = new CourseSection { Title = "Module 1: Essential Patterns", OrderIndex = 1 };
            sec3_1.Lessons.Add(new Lesson
            {
                Title = "The Power of Strategy and Observer Patterns",
                Description = "Hands-on walkthrough of dynamic pricing calculations and reactive progress tracking.",
                VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
                DurationSeconds = 300,
                OrderIndex = 1,
                IsFreePreview = true,
                ContentNotes = "Design patterns provide proven vocabularies to solve recurring software challenges."
            });
            course3.Sections.Add(sec3_1);

            await context.Courses.AddRangeAsync(course1, course2, course3);
            await context.SaveChangesAsync();

            // Seed Reviews for Course 1
            var r1 = new Review
            {
                CourseId = course1.Id,
                StudentId = studentUser!.Id,
                Rating = 5,
                Comment = "Outstanding course! The separation between layers and the practical implementation of the Strategy and Observer patterns made everything crystal clear.",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                HelpfulVotesCount = 4
            };
            await context.Reviews.AddAsync(r1);

            // Seed an active Enrollment for the student in Course 1 with partial progress
            var enrollment1 = new Enrollment
            {
                StudentId = studentUser.Id,
                CourseId = course1.Id,
                EnrolledAt = DateTime.UtcNow.AddDays(-7),
                ProgressPercentage = 33.3,
                IsCompleted = false
            };
            await context.Enrollments.AddAsync(enrollment1);
            await context.SaveChangesAsync();

            // Mark first lesson completed for this enrollment
            var firstLesson = course1.Sections.First().Lessons.First();
            var completion1 = new LessonCompletion
            {
                EnrollmentId = enrollment1.Id,
                LessonId = firstLesson.Id,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow.AddDays(-5),
                LastWatchPositionSeconds = 300
            };
            await context.LessonCompletions.AddAsync(completion1);

            // Seed a note for the student
            var note1 = new LessonNote
            {
                StudentId = studentUser.Id,
                LessonId = firstLesson.Id,
                TimestampSeconds = 45,
                Content = "Important: The presentation layer should never invoke EF DbContext directly. Always delegate through services and Unit of Work."
            };
            await context.LessonNotes.AddAsync(note1);

            // Seed a bookmark
            var bookmark1 = new LessonBookmark
            {
                StudentId = studentUser.Id,
                LessonId = firstLesson.Id,
                Note = "Review architecture diagram before final exam."
            };
            await context.LessonBookmarks.AddAsync(bookmark1);
        }

        // 5. Seed Coupons
        if (!await context.Coupons.AnyAsync())
        {
            var coupons = new List<Coupon>
            {
                new Coupon
                {
                    Code = "WELCOME50",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 50m,
                    ExpiryDate = DateTime.UtcNow.AddMonths(6),
                    UsageLimit = 500,
                    TimesUsed = 23,
                    IsActive = true
                },
                new Coupon
                {
                    Code = "SAVE20",
                    DiscountType = DiscountType.FixedAmount,
                    DiscountValue = 20m,
                    ExpiryDate = DateTime.UtcNow.AddMonths(3),
                    UsageLimit = 200,
                    TimesUsed = 12,
                    IsActive = true,
                    MinimumSpend = 40m
                },
                new Coupon
                {
                    Code = "BLACKFRIDAY",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 30m,
                    ExpiryDate = DateTime.UtcNow.AddMonths(12),
                    UsageLimit = 1000,
                    TimesUsed = 78,
                    IsActive = true
                }
            };

            await context.Coupons.AddRangeAsync(coupons);
            await context.SaveChangesAsync();
        }
    }
}
