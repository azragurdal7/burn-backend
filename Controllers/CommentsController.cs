using BurnAnalysisApp.Data;
using BurnAnalysisApp.Models;
using BurnAnalysisApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BurnAnalysisApp.Controllers
{
    [Route("api/comments")]
    [ApiController]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public CommentsController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(Comment comment)
        {
            comment.CreatedAt = DateTime.UtcNow;
            // Yorumu yapan doktorun adını doğrudan modelden alıyoruz.
            // Doktor ID'sini de alıp kaydetmek isteyebilirsiniz ilerideki sorgular için.
            var currentDoctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Yorumu yapan doktorun adını al
            var commentingDoctor = await _context.Doctors.FindAsync(currentDoctorId);
            if (commentingDoctor != null)
            {
                comment.DoctorName = commentingDoctor.Name;
            }

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Bildirim oluştur
            var forumPost = await _context.ForumPosts.Include(fp => fp.Patient).FirstOrDefaultAsync(fp => fp.ForumPostID == comment.ForumPostID);
            if (forumPost != null)
            {
                var postOwnerDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Name == forumPost.DoctorName);
                if (postOwnerDoctor != null && !string.IsNullOrEmpty(postOwnerDoctor.Email))
                {
                    string notificationMessage = $"{comment.DoctorName} adlı doktor, gönderinize bir yorum yaptı: \"{comment.Content?.Substring(0, Math.Min(comment.Content.Length, 50))}\"";
                    var notification = new Notification
                    {
                        DoctorID = postOwnerDoctor.DoctorID, // Bildirimi post sahibine gönderiyoruz
                        Message = notificationMessage,
                        ForumPostID = comment.ForumPostID,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    string emailSubject = $"Yeni Yorum Bildirimi: {forumPost.Patient?.Name} Hakkında";
                    string emailMessage = $"{comment.DoctorName} adlı doktor, gönderinize bir yorum yaptı: \"{comment.Content}\"\n\nGönderiyi görmek için: [Gönderi Linki Buraya]"; // TODO: Gönderi linki ekle
                    await _emailService.SendEmailToDoctorAsync(postOwnerDoctor.Email, postOwnerDoctor.Name, emailSubject, emailMessage);
                }
                else
                {
                    Console.WriteLine($"Hata: Post sahibi doktor bulunamadı veya e-posta adresi boş (yorum ekleme). Post ID: {forumPost.ForumPostID}, Doktor Adı: {forumPost.DoctorName}");
                }
            }

            return Ok(new { Message = "Yorum başarıyla eklendi." });
        }
    }
}