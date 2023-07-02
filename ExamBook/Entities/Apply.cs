using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ExamBook.Identity.Entities;

namespace ExamBook.Entities
{
	public class Apply:Entity
	{
		public string Code { get; set; } = "";
		public string NormalizedCode { get; set; } = "";
		public string FirstName { get; set; } = "";
		public string LastName { get; set; } = "";

		public char Sex { get; set; } = 'M';
		public DateTime BirthDate { get; set; }

		public Space Space { get; set; } = null!;
		public ulong? SpaceId { get; set; }

		public Student Student { get; set; } = null!;
		public ulong? StudentId { get; set; }

		public string UserId { get; set; } = "";

		[NotMapped] public User User { get; set; } = null!;

		public DateTime? AcceptedAt { get; set; }
		public bool IsAccepted => AcceptedAt != null;
		
		public DateTime? RejectedAt { get; set; }
		public bool IsRejected => RejectedAt != null;

		public List<StudentSpeciality> Specialities { get; set; } = new();
	}


	public class ApplySpeciality : Entity
	{
		public ApplySpeciality() {}
		public ApplySpeciality(Apply apply, Speciality speciality)
		{
			Apply = apply;
			Speciality = speciality;
		}

		public Apply? Apply { get; set; }
		public ulong? ApplyId { get; set; }

		public Speciality? Speciality { get; set; }
		public ulong SpecialityId { get; set; }
	}
}