namespace ExamBook.Entities
{
	public class Classroom:Entity
	{
		public string Name { get; set; } = "";
		public string NormalizedName { get; set; } = "";

		public Room? Room { get; set; } = null!;
		public ulong RoomId { get; set; }

		public Space Space { get; set; } = null!;
		public ulong SpaceId { get; set; }
	}


	public class ClassroomSpeciality : Entity
	{
		public ClassroomSpeciality()
		{ }

		public ClassroomSpeciality(Classroom? classroom, Speciality? speciality)
		{
			Classroom = classroom;
			Speciality = speciality;
		}



		public Classroom? Classroom { get; set; }
		public ulong? ClassroomId { get; set; }

		public Speciality? Speciality { get; set; }
		public ulong SpecialityId { get; set; }
	}
}