    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace BurnAnalysisApp.Models
    {
        public class PatientInfo
        {
            [Key]
            public int PatientID { get; set; }

            [Required]
            public string Name { get; set; } = string.Empty;
            public string? Email { get; set; }

            [Range(0, 120)]
            public int Age { get; set; }

            [Required]
            public string Gender { get; set; } = string.Empty;

            public string? MedicalHistory { get; set; }
            public string? BurnCause { get; set; }
            public string? BurnArea { get; set; }

            public DateTime? HospitalArrivalDate { get; set; }
            public DateTime? BurnOccurrenceDate { get; set; }
            public string? PhotoPath { get; set; }
            public string? BurnDepth { get; set; }
            public double? BurnPercentage { get; set; }  // Yeni özellik eklendi

            [Display(Name = "Yanık Boyutu (cm²)")]
            [Range(0.1, 10000, ErrorMessage = "Yanık boyutu 0.1 cm² ile 10000 cm² arasında olmalıdır.")] // Örnek bir aralık, ihtiyaca göre ayarlayın
            public double? BurnSizeCm2 { get; set; } // Santimetre kare cinsinden yanık boyutu
            public string? AudioPath { get; set; } // Ses dosyasının yolunu saklamak için
            public bool ReminderSent { get; set; } = false; // Yeni alan
            public bool Verified { get; set; } = false;  // Yeni sütun
            public bool Trained { get; set; } = false;  //  Yeni alan

             [Display(Name = "Boy (cm)")]
            [Range(10, 230, ErrorMessage = "Boy 10 cm ile 230 cm arasında olmalıdır.")] // Örnek bir aralık
            public double? HeightCm { get; set; } // Santimetre cinsinden boy

            [Display(Name = "Kilo (kg)")]
            [Range(1, 200, ErrorMessage = "Kilo 1 kg ile 200 kg arasında olmalıdır.")] // Örnek bir aralık
            public double? WeightKg { get; set; } // Kilogram cinsinden kilo
            }
    }
