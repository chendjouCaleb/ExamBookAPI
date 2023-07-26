using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social.Helpers;
using Traceability.Asserts;

namespace ExamBookTest.Services
{
	public class ParticipantServiceTest
	{
		private ParticipantService _participantService = null!;
		private EventAssertionsBuilder _eventAssertionsBuilder = null!;
		private ApplicationDbContext _dbContext = null!;


		private Space _space  = null!;
		private Examination _examination  = null!;
		private Student _student1  = null!;
		private Student _student2  = null!;
		private Speciality _speciality = null!;
		private User _adminUser  = null!;

		[SetUp]
		public async Task Setup()
		{
			var provider = new ServiceCollection().Setup();
			_eventAssertionsBuilder = provider.GetRequiredService<EventAssertionsBuilder>();
			_dbContext = provider.GetRequiredService<ApplicationDbContext>();

			var userService = provider.GetRequiredService<UserService>();
			var spaceService = provider.GetRequiredService<SpaceService>();
			var examinationService = provider.GetRequiredService<ExaminationService>();
			var studentService = provider.GetRequiredService<StudentService>();
			var specialityService = provider.GetRequiredService<SpecialityService>();
			_participantService = provider.GetRequiredService<ParticipantService>();

			_adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
			
			var result = await spaceService.AddAsync(_adminUser.Id, new SpaceAddModel {
				Name = "UY-1, PHILOSOPHY, L1",
				Identifier = "uy1_phi_l1"
			});
			_space = result.Item;
			
			var specialityModel = new SpecialityAddModel {Name = "speciality name"};
			_speciality = (await specialityService.AddSpecialityAsync(_space, specialityModel, _adminUser)).Item;

			_student1 = (await studentService.AddAsync(_space, new StudentAddModel
			{
				FirstName = "first name",
				LastName = "last name",
				Code = "8say6g3",
				BirthDate = new DateTime(1990, 1, 1),
				Sex = 'm',
				SpecialityIds = new HashSet<ulong> {_speciality.Id }
			}, _adminUser)).Item;
			
			_student2 = (await studentService.AddAsync(_space, new StudentAddModel
			{
				FirstName = "first name",
				LastName = "last name",
				Code = "8saDE6g3",
				BirthDate = new DateTime(1990, 1, 1),
				Sex = 'm'
			}, _adminUser)).Item;
			
			

			var examinationAddModel = new ExaminationAddModel
			{
				Name = "Examination name",
				StartAt = DateTime.Now.AddDays(-12),
				SpecialityIds = new HashSet<ulong> {_speciality.Id }
			};
			_examination = (await examinationService.AddAsync(_space, examinationAddModel, _adminUser)).Item;

		}

		[Test]
		public async Task GetByIdAsync()
		{
			var students = new HashSet<Student> { _student1};
			var participants =(await _participantService.AddAsync(_examination, students, _adminUser)).Item;
			var participant = participants.First();
			var result= await _participantService.GetByIdAsync(participant.Id);
			
			Assert.AreEqual(participant.Id, result.Id);
		}


		[Test]
		public void TryGet_NotFound_ShouldThrow()
		{
			var participantId = ulong.MaxValue;
			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			{
				await _participantService.GetByIdAsync(participantId);
			});

			Assert.AreEqual("ParticipantNotFoundById", ex!.Code);
			Assert.AreEqual(participantId, ex.Params[0]);
		}
		
		
		// [Test]
		// public async Task GetByCodeAsync()
		// {
		// 	var participant =(await _participantService.AddAsync(_examination, _model)).Item;
		// 	var result = await _participantService.GetByCodeAsync(_examination, participant.Code);
		// 	
		// 	Assert.AreEqual(participant.Id, result.Id);
		// 	Assert.AreEqual(participant.Code, result.Code);
		// }
		//
		// [Test]
		// public void TryGetByCode_NotFound_ShouldThrow()
		// {
		// 	var participantCode = Guid.NewGuid().ToString();
		// 	var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
		// 	{
		// 		await _participantService.GetByCodeAsync(_examination, participantCode);
		// 	});
		//
		// 	Assert.AreEqual("ParticipantNotFoundById", ex!.Code);
		// 	Assert.AreEqual(participantCode, ex.Params[0]);
		// 	Assert.AreEqual(_examination.Name, ex.Params[1]);
		// }
		//
		//
		// [Test]
		// public async Task ContainsByCodeAsync()
		// {
		// 	var participant =(await _participantService.AddAsync(_examination, _model)).Item;
		// 	var result = await _participantService.ContainsByCodeAsync(_examination, participant.Code);
		// 	Assert.True(result);
		// }
		//
		//
		// [Test]
		// public async Task ContainsByCode_WithNotFound_ShouldBeFalse()
		// {
		// 	var participantCode = Guid.NewGuid().ToString();
		// 	var participant =(await _participantService.AddAsync(_examination, _model)).Item;
		// 	var result = await _participantService.ContainsByCodeAsync(_examination, participantCode);
		// 	Assert.False(result);
		// }

		
		[Test]
		public async Task AddParticipant()
		{
			var students = new HashSet<Student> {_student1, _student2};
			var result = await _participantService.AddAsync(_examination, students, _adminUser);
			var participants = result.Item;
			var eventTest = _eventAssertionsBuilder.Build(result.Event);

			Assert.AreEqual(students.Count, participants.Count);

			foreach (var participant in participants)
			{
				var student = students.FirstOrDefault(s => s.Id == participant.Id);
				Assert.NotNull(student);
				
				Assert.AreEqual(_examination.Id, participant.ExaminationId);
				await eventTest.HasPublisherIdAsync(student!.PublisherId);
				await eventTest.HasPublisherIdAsync(participant.PublisherId);

				var specialities = await _dbContext.ParticipantSpecialities
					.Include(ps => ps.ExaminationSpeciality)
					.Include(ps => ps.StudentSpeciality)
					.Where(s => s.ParticipantId == participant.Id)
					.ToListAsync();

				foreach (var participantSpeciality in specialities)
				{
					var studentSpeciality = await _dbContext.StudentSpecialities
						.Where(s => s.StudentId == student.Id)
						.Where(s => s.SpecialityId == participantSpeciality.StudentSpecialityId)
						.FirstOrDefaultAsync();
					
					Assert.AreEqual(participant.Id, participantSpeciality.ParticipantId);
					Assert.AreEqual(studentSpeciality!.Id, participantSpeciality.StudentSpecialityId);
				}

			}

			
			eventTest.HasName("PARTICIPANTS_ADD");
			
			await eventTest.HasActorIdAsync(_adminUser.ActorId);
			await eventTest.HasPublisherIdAsync(_space.PublisherId);
			await eventTest.HasPublisherIdAsync(_examination.PublisherId);
			eventTest.HasData(participants.Select(p => p.Id));
		}
	}
}