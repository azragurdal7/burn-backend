using Microsoft.AspNetCore.Mvc;
using BurnAnalysisApp.Data;
using BurnAnalysisApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // IConfiguration için
using BurnAnalysisApp.Services; // EmailService için (varsayım)
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // IFormFile için
using Microsoft.AspNetCore.Hosting; // IWebHostEnvironment için (wwwroot yolu)
using System; // Guid için

namespace BurnAnalysisApp.Controllers
{
    // DTO'ları ya buraya ya da Models/Dtos klasörüne taşıyın.
    // Controller dosyası içinde namespace seviyesinde olmaları daha iyi.
    public class AiBurnAnalysisUpdateDto
    {
        public string? BurnDepth { get; set; }
        public double? BurnSizeCm2 { get; set; }
        public double? BurnPercentage { get; set; }
    }

    public class BurnDepthUpdateDto
    {
        public string? BurnDepth { get; set; }
    }

    public class VerifiedUpdateDto
    {
        public bool Verified { get; set; }
    }
    // --- DTO Tanımları Bitiş ---


    [Route("api/[controller]")]
    [ApiController]
    public partial class PatientController : ControllerBase // "partial" EKLENDİ
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PatientController(AppDbContext context, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        // Controller'ın action metodları aşağıda veya bu partial blok içinde olabilir.
        // Eğer aşağıdaki gibi ayrı bir partial blok kullanacaksanız, ikisi de partial olmalı.
    }

    // Controller sınıfının Action Metodlarını içeren kısmı
    public partial class PatientController // Bu zaten partial idi
    {
        [HttpGet("search")]
        public async Task<IActionResult> SearchPatients([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest("Arama terimi boş olamaz.");
            }

            var patients = await _context.Patients
                .Where(p => p.Name != null && p.Name.ToLower().Contains(searchTerm.ToLower()))
                .ToListAsync();

            return Ok(patients);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatientById(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound("Hasta bulunamadı.");
            }
            return Ok(patient);
        }

        [HttpGet]
        public async Task<IActionResult> GetPatients()
        {
            var patients = await _context.Patients.OrderByDescending(p => p.PatientID).ToListAsync();
            return Ok(patients);
        }

        [HttpPost]
        public async Task<IActionResult> AddPatient([FromForm] PatientInfo patient, [FromForm] IFormFile? photoFile)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (photoFile != null && photoFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                // wwwroot yoksa veya boşsa fallback (genellikle geliştirme ortamında olur)
                if (string.IsNullOrEmpty(wwwRootPath)) wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                
                // Dosyaları daha organize tutmak için alt klasörler
                string patientPhotosFolder = Path.Combine(wwwRootPath, "uploads");
                if (!Directory.Exists(patientPhotosFolder)) Directory.CreateDirectory(patientPhotosFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photoFile.FileName);
                var filePath = Path.Combine(patientPhotosFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }
                patient.PhotoPath = Path.Combine("uploads", uniqueFileName).Replace("\\", "/");
            }

            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPatientById), new { id = patient.PatientID }, patient);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromForm] PatientInfo patientUpdates, [FromForm] IFormFile? photoFile)
        {
            if (id != patientUpdates.PatientID)
            {
                return BadRequest("ID uyuşmazlığı.");
            }
             // ModelState'i burada kontrol etmek daha mantıklı
            if (!ModelState.IsValid) // Bu, patientUpdates nesnesinin DataAnnotation'larını kontrol eder.
            {
                 return BadRequest(ModelState);
            }


            var existingPatient = await _context.Patients.AsTracking().FirstOrDefaultAsync(p => p.PatientID == id);
            if (existingPatient == null)
            {
                return NotFound("Hasta bulunamadı.");
            }

            // Gelen değerleri mevcut hasta üzerine ata (AutoMapper gibi bir kütüphane de kullanılabilir)
            existingPatient.Name = patientUpdates.Name;
            existingPatient.Email = patientUpdates.Email;
            existingPatient.Age = patientUpdates.Age;
            existingPatient.Gender = patientUpdates.Gender;
            existingPatient.MedicalHistory = patientUpdates.MedicalHistory;
            existingPatient.BurnCause = patientUpdates.BurnCause;
            existingPatient.BurnArea = patientUpdates.BurnArea;
            existingPatient.HospitalArrivalDate = patientUpdates.HospitalArrivalDate;
            existingPatient.BurnOccurrenceDate = patientUpdates.BurnOccurrenceDate;
            existingPatient.BurnDepth = patientUpdates.BurnDepth;
            existingPatient.BurnPercentage = patientUpdates.BurnPercentage;
            existingPatient.BurnSizeCm2 = patientUpdates.BurnSizeCm2;
            existingPatient.HeightCm = patientUpdates.HeightCm;
            existingPatient.WeightKg = patientUpdates.WeightKg;
            // Verified, Trained, ReminderSent gibi alanlar ayrı endpointlerle yönetiliyorsa burada güncellenmemeli.
            // Eğer bu formdan güncellenmesi gerekiyorsa, onlar için de atama yapılmalı.

            if (photoFile != null && photoFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (string.IsNullOrEmpty(wwwRootPath)) wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string patientPhotosFolder = Path.Combine(wwwRootPath, "uploads");
                 if (!Directory.Exists(patientPhotosFolder)) Directory.CreateDirectory(patientPhotosFolder);


                // Eski fotoğrafı sil (varsa)
                if (!string.IsNullOrEmpty(existingPatient.PhotoPath))
                {
                    string oldFullPath = Path.Combine(wwwRootPath, existingPatient.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFullPath))
                    {
                        try { System.IO.File.Delete(oldFullPath); }
                        catch (IOException ex) { Console.WriteLine($"Eski fotoğraf silinirken hata: {ex.Message}"); }
                    }
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photoFile.FileName);
                var filePath = Path.Combine(patientPhotosFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }
                existingPatient.PhotoPath = Path.Combine("uploads", uniqueFileName).Replace("\\", "/");
            }

            try
            {
                // _context.Patients.Update(existingPatient); // AsTracking() kullandığımız için EF Core değişiklikleri zaten izliyor.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Patients.Any(e => e.PatientID == id)) return NotFound();
                else throw;
            }
            return Ok(existingPatient);
        }

        [HttpPost("update-patient-burndepth/{id}")]
        public async Task<IActionResult> UpdateSingleBurnDepth(int id, [FromBody] BurnDepthUpdateDto dto)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound("Hasta bulunamadı.");
            patient.BurnDepth = dto.BurnDepth;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Yanık derinliği başarıyla güncellendi." });
        }

        [HttpPost("update-verified/{id}")]
        public async Task<IActionResult> UpdateVerifiedStatus(int id, [FromBody] VerifiedUpdateDto dto)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound("Hasta bulunamadı.");
            patient.Verified = dto.Verified;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Verified durumu güncellendi: {dto.Verified}" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound("Hasta bulunamadı.");

            string wwwRootPath = _webHostEnvironment.WebRootPath;
             if (string.IsNullOrEmpty(wwwRootPath)) wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            if (!string.IsNullOrEmpty(patient.PhotoPath))
            {
                string fullPhotoPath = Path.Combine(wwwRootPath, patient.PhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(fullPhotoPath))
                {
                     try { System.IO.File.Delete(fullPhotoPath); }
                     catch (IOException ex) { Console.WriteLine($"Fotoğraf silinirken hata: {ex.Message}"); }
                }
            }
            if (!string.IsNullOrEmpty(patient.AudioPath))
            {
                string fullAudioPath = Path.Combine(wwwRootPath, patient.AudioPath.TrimStart('/'));
                if (System.IO.File.Exists(fullAudioPath))
                {
                    try { System.IO.File.Delete(fullAudioPath); }
                    catch (IOException ex) { Console.WriteLine($"Ses dosyası silinirken hata: {ex.Message}"); }
                }
            }

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/send-reminder")]
        public async Task<IActionResult> SendAppointmentReminder(int id, [FromBody] DateTime appointmentDate)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound("Hasta bulunamadı.");
            if (string.IsNullOrEmpty(patient.Email)) return BadRequest("Hastanın e-posta adresi bulunamadı.");

            var emailService = new EmailService(_configuration); // DI ile enjekte etmek daha iyi olur.
            try
            {
                await emailService.SendAppointmentReminderEmailAsync(patient.Email, patient.Name ?? "Hasta", appointmentDate);
                patient.ReminderSent = true;
                await _context.SaveChangesAsync();
                return Ok("Hatırlatma e-postası başarıyla gönderildi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email gönderirken hata: {ex.Message}");
                return StatusCode(500, "E-posta gönderilirken bir sorun oluştu.");
            }
        }

        [HttpPost("upload-audio/{patientId}")]
        public async Task<IActionResult> UploadAudio([FromForm] IFormFile audioFile, [FromRoute] int patientId)
        {
            if (audioFile == null || audioFile.Length == 0) return BadRequest("Ses dosyası yüklenmedi.");

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound("Hasta bulunamadı.");

            string wwwRootPath = _webHostEnvironment.WebRootPath;
            if (string.IsNullOrEmpty(wwwRootPath)) wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            
            string patientAudioFolder = Path.Combine(wwwRootPath, "uploads", "Audio");
            if (!Directory.Exists(patientAudioFolder)) Directory.CreateDirectory(patientAudioFolder);

            if (!string.IsNullOrEmpty(patient.AudioPath))
            {
                string oldFullPath = Path.Combine(wwwRootPath, patient.AudioPath.TrimStart('/'));
                if (System.IO.File.Exists(oldFullPath))
                {
                    try { System.IO.File.Delete(oldFullPath); }
                    catch (IOException ex) { Console.WriteLine($"Eski ses dosyası silinirken hata: {ex.Message}"); }
                }
            }

            var uniqueFileName = $"{patientId}_{Guid.NewGuid()}{Path.GetExtension(audioFile.FileName)}";
            var relativePath = Path.Combine("uploads","Audio", uniqueFileName).Replace("\\", "/");
            var filePath = Path.Combine(patientAudioFolder, uniqueFileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }
                patient.AudioPath = relativePath;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Ses kaydı başarıyla yüklendi.", filePath = patient.AudioPath });
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(filePath))
                {
                    try { System.IO.File.Delete(filePath); }
                    catch (IOException ioEx) { Console.WriteLine($"Başarısız yükleme sonrası ses dosyası silinirken hata: {ioEx.Message}"); }
                }
                Console.WriteLine($"Ses kaydı yüklenirken hata: {ex.ToString()}");
                return StatusCode(500, $"Ses kaydı yüklenirken bir hata oluştu: {ex.Message}");
            }
        }

        [HttpDelete("delete-audio/{patientId}")]
        public async Task<IActionResult> DeleteAudio(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound("Hasta bulunamadı.");

            if (!string.IsNullOrEmpty(patient.AudioPath))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (string.IsNullOrEmpty(wwwRootPath)) wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string fullPath = Path.Combine(wwwRootPath, patient.AudioPath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    try { System.IO.File.Delete(fullPath); }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Ses dosyası silinirken hata: {ex.Message}");
                        return StatusCode(500, "Dosya silinirken bir hata oluştu.");
                    }
                }
            }
            patient.AudioPath = null;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("update-ai-analysis/{id}")]
        public async Task<IActionResult> UpdateAiAnalysisResults(int id, [FromBody] AiBurnAnalysisUpdateDto dto)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound("Hasta bulunamadı.");

            bool changed = false;
            if (dto.BurnDepth != null && patient.BurnDepth != dto.BurnDepth)
            {
                patient.BurnDepth = dto.BurnDepth;
                changed = true;
            }
            if (dto.BurnSizeCm2.HasValue && (!patient.BurnSizeCm2.HasValue || Math.Abs(patient.BurnSizeCm2.Value - dto.BurnSizeCm2.Value) > 0.001))
            {
                patient.BurnSizeCm2 = dto.BurnSizeCm2.Value;
                changed = true;
            }
            if (dto.BurnPercentage.HasValue && (!patient.BurnPercentage.HasValue || Math.Abs(patient.BurnPercentage.Value - dto.BurnPercentage.Value) > 0.001))
            {
                patient.BurnPercentage = dto.BurnPercentage.Value;
                changed = true;
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"Hasta ID: {id} AI Analiz Sonuçları Güncellendi: Derinlik='{dto.BurnDepth}', Alan={dto.BurnSizeCm2}, Yüzde={dto.BurnPercentage}");
            }
            else
            {
                Console.WriteLine($"Hasta ID: {id} AI Analiz Sonuçları için güncelleme yapılmadı (gelen değerler mevcutla aynı veya boş).");
            }
            return Ok(new { message = "Yapay zeka analiz sonuçları başarıyla güncellendi." });
        }
    }
}