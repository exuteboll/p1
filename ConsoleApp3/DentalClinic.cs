﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ConsoleApp3
{
    internal class DentalClinic
    {
        public List<Doctor> Doctors { get; set; } = new List<Doctor>();
        public List<Patient> Patients { get; set; } = new List<Patient>();
        public List<Service> Services { get; set; } = new List<Service>();
        public List<VisitRecord> VisitRecords { get; set; } = new List<VisitRecord>();
        public List<Payment> Payments { get; set; } = new List<Payment>();

        private int _nextDoctorId = 1;
        private int _nextPatientId = 1;
        private int _nextServiceId = 1;
        private int _nextVisitId = 1;
        private int _nextPaymentId = 1;

        public DentalClinic()
        {
            LoadData();
            UpdateNextIds();
        }

        private void UpdateNextIds()
        {
            _nextDoctorId = Doctors.Any() ? Doctors.Max(d => d.Id) + 1 : 1;
            _nextPatientId = Patients.Any() ? Patients.Max(p => p.Id) + 1 : 1;
            _nextServiceId = Services.Any() ? Services.Max(s => s.Id) + 1 : 1;
            _nextVisitId = VisitRecords.Any() ? VisitRecords.Max(v => v.Id) + 1 : 1;
            _nextPaymentId = Payments.Any() ? Payments.Max(p => p.Id) + 1 : 1;
        }

        public void SaveData()
        {
            SaveToFile("doctors.txt", Doctors.Select(d => d.ToFileString()));
            SaveToFile("patients.txt", Patients.Select(p => p.ToFileString()));
            SaveToFile("services.txt", Services.Select(s => s.ToFileString()));
            SaveToFile("visits.txt", VisitRecords.Select(v => v.ToFileString()));
            SaveToFile("payments.txt", Payments.Select(p => p.ToFileString()));
        }

        private void SaveToFile(string filename, IEnumerable<string> lines)
        {
            File.WriteAllLines(filename, lines);
        }

        public void LoadData()
        {
            Doctors = LoadFromFile("doctors.txt", Doctor.FromFileString);
            Patients = LoadFromFile("patients.txt", Patient.FromFileString);
            Services = LoadFromFile("services.txt", Service.FromFileString);
            VisitRecords = LoadFromFile("visits.txt", VisitRecord.FromFileString);
            Payments = LoadFromFile("payments.txt", Payment.FromFileString);
        }

        private List<T> LoadFromFile<T>(string filename, Func<string, T> converter)
        {
            if (!File.Exists(filename)) return new List<T>();

            try
            {
                return File.ReadAllLines(filename)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(converter)
                    .ToList();
            }
            catch
            {
                return new List<T>();
            }
        }




        public IEnumerable<Doctor> GetDoctorsWorkingOnDay(DayOfWeek day)
        {
            return Doctors.Where(d => d.WorkSchedule.ContainsKey(day));
        }


        public IEnumerable<VisitRecord> GetTodaysAppointments()
        {
            var today = DateTime.Today;
            return VisitRecords.Where(v => v.Date.Date == today)
                              .OrderBy(v => v.Time);
        }

 
        public IEnumerable<(Service Service, int Count)> GetMostPopularServices(int topN = 5)
        {
            return VisitRecords.GroupBy(v => v.ServiceId)
                              .Select(g => (Services.First(s => s.Id == g.Key), g.Count()))
                              .OrderByDescending(x => x.Item2)
                              .Take(topN);
        }


        public IEnumerable<(Patient Patient, decimal Debt)> GetPatientsWithDebt()
        {
            return Patients.Select(patient =>
            {
     
                var totalServicesCost = patient.VisitHistoryIds
                    .Join(VisitRecords, id => id, visit => visit.Id, (id, visit) => visit)
                    .Where(visit => visit.IsCompleted)
                    .Sum(visit => Services.First(service => service.Id == visit.ServiceId).Price);

    
                var totalPayments = patient.PaymentIds
                    .Join(Payments, id => id, payment => payment.Id, (id, payment) => payment)
                    .Sum(payment => payment.Amount);

        
                return (patient, Debt: totalServicesCost - totalPayments);
            })
            .Where(result => result.Debt > 0);
        }

  
        public decimal GetMonthlyIncome(int year, int month)
        {
            return Payments
       .Where(payment => payment.Date.Year == year && payment.Date.Month == month)
       .Sum(payment => payment.Amount);
        }

     
        public decimal GetAveragePayment()
        {
            return Payments.Any() ? Payments.Average(p => p.Amount) : 0;
        }

   
        public void AddDoctor(Doctor doctor)
        {
            doctor.Id = _nextDoctorId++;
            Doctors.Add(doctor);
            SaveData();
        }

        public bool RemoveDoctor(int doctorId)
        {
            var doctor = Doctors.FirstOrDefault(d => d.Id == doctorId);
            if (doctor != null && !VisitRecords.Any(v => v.DoctorId == doctorId))
            {
                Doctors.Remove(doctor);
                SaveData();
                return true;
            }
            return false;
        }

        public void AddPatient(Patient patient)
        {
            patient.Id = _nextPatientId++;
            Patients.Add(patient);
            SaveData();
        }

        public void AddService(Service service)
        {
            service.Id = _nextServiceId++;
            Services.Add(service);
            SaveData();
        }

        public void AddVisitRecord(VisitRecord visit)
        {
            visit.Id = _nextVisitId++;
            VisitRecords.Add(visit);

            var patient = Patients.FirstOrDefault(p => p.Id == visit.PatientId);
            if (patient != null)
            {
                patient.VisitHistoryIds.Add(visit.Id);
            }

            SaveData();
        }

        public void AddPayment(Payment payment)
        {
            payment.Id = _nextPaymentId++;
            Payments.Add(payment);

            var patient = Patients.FirstOrDefault(p => p.Id == payment.PatientId);
            if (patient != null)
            {
                patient.PaymentIds.Add(payment.Id);
            }

            SaveData();
        }
        public bool RemovePatient(int patientId)
        {
            var patient = Patients.FirstOrDefault(p => p.Id == patientId);
            if (patient == null) return false;


            if (VisitRecords.Any(v => v.PatientId == patientId) ||
                Payments.Any(p => p.PatientId == patientId))
            {
                return false;
            }

            Patients.Remove(patient);

         
            if (patientId == _nextPatientId - 1)
            {
                _nextPatientId = Patients.Any() ? Patients.Max(p => p.Id) + 1 : 1;
            }

            SaveData();
            return true;
        }
        public bool RemoveService(int serviceId)
        {
            var service = Services.FirstOrDefault(s => s.Id == serviceId);
            if (service == null) return false;

           
            if (VisitRecords.Any(v => v.ServiceId == serviceId))
            {
                return false;
            }

            Services.Remove(service);

      
            if (serviceId == _nextServiceId - 1)
            {
                _nextServiceId = Services.Any() ? Services.Max(s => s.Id) + 1 : 1;
            }

            SaveData();
            return true;
        }
    }
}
