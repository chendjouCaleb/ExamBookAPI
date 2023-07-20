// using System;
// using System.Threading.Tasks;
// using ExamBook.Entities;
// using ExamBook.Exceptions;
// using ExamBook.Identity.Entities;
// using ExamBook.Models;
// using ExamBook.Models.Data;
// using ExamBook.Persistence;
// using ExamBook.Services;
// using Social.Helpers;
// using Traceability.Asserts;
// using Traceability.Services;
//
// namespace ExamBookTest.Services
// {
// 	public class ParticipantServiceTest
// 	{
// 		private ParticipantService _participantService;
// 		private PublisherService _publisherService;
// 		private EventAssertionsBuilder _eventAssertionsBuilder;
// 		private ApplicationDbContext _dbContext;
//
//
// 		private Space _space;
// 		private Examination _examination;
// 		private Student _student;
// 		private Student _studentWithUser;
// 		private User _adminUser;
// 		private User _user;
// 		private ParticipantAddModel _model = new ParticipantAddModel()
// 		{
// 			FirstName = "First name",
// 			LastName = "Last name",
// 			Sex = 'F',
// 			BirthDate = new DateOnly(2000, 10, 10),
// 			Code = "D6F2E8"
// 		};
//
//
// 		[Test]
// 		public async Task GetByIdAsync()
// 		{
// 			var participant =(await _participantService.AddAsync(_examination, _model)).Item;
// 			var result= await _participantService.GetByIdAsync(participant.Id);
// 			
// 			Assert.AreEqual(participant.Id, result.Id);
// 			Assert.AreEqual(participant.Code, result.Code);
// 		}
//
//
// 		[Test]
// 		public void TryGet_NotFound_ShouldThrow()
// 		{
// 			var participantId = ulong.MaxValue;
// 			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
// 			{
// 				await _participantService.GetByIdAsync(participantId);
// 			});
//
// 			Assert.AreEqual("ParticipantNotFoundById", ex!.Code);
// 			Assert.AreEqual(participantId, ex.Params[0]);
// 		}
// 		
// 		
// 		[Test]
// 		public async Task GetByCodeAsync()
// 		{
// 			var participant =(await _participantService.AddAsync(_examination, _model)).Item;
// 			var result = await _participantService.GetByCodeAsync(_examination, participant.Code);
// 			
// 			Assert.AreEqual(participant.Id, result.Id);
// 			Assert.AreEqual(participant.Code, result.Code);
// 		}
//
// 		[Test]
// 		public void TryGetByCode_NotFound_ShouldThrow()
// 		{
// 			var participantCode = Guid.NewGuid().ToString();
// 			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
// 			{
// 				await _participantService.GetByCodeAsync(_examination, participantCode);
// 			});
//
// 			Assert.AreEqual("ParticipantNotFoundById", ex!.Code);
// 			Assert.AreEqual(participantCode, ex.Params[0]);
// 			Assert.AreEqual(_examination.Name, ex.Params[1]);
// 		}
// 		
// 		
// 		[Test]
// 		public async Task ContainsByCodeAsync()
// 		{
// 			var participant =(await _participantService.AddAsync(_examination, _model)).Item;
// 			var result = await _participantService.ContainsByCodeAsync(_examination, participant.Code);
// 			Assert.True(result);
// 		}
// 		
// 		
// 		[Test]
// 		public async Task ContainsByCode_WithNotFound_ShouldBeFalse()
// 		{
// 			var participantCode = Guid.NewGuid().ToString();
// 			var participant =(await _participantService.AddAsync(_examination, _model)).Item;
// 			var result = await _participantService.ContainsByCodeAsync(_examination, participantCode);
// 			Assert.False(result);
// 		}
//
// 		[Test]
// 		public async Task ContainsByUser()
// 		{
// 			
// 		}
//
// 		[Test]
// 		public async Task AddParticipant()
// 		{
// 			var result = await _participantService.AddAsync(_examination, _model);
// 			var participant = result.Item;
// 			
// 			Assert.AreEqual(_model.Code, participant.Code);
// 			Assert.AreEqual(StringHelper.Normalize(_model.Code), participant.NormalizedCode);
// 			Assert.AreEqual(_model.FirstName, participant.FirstName);
// 			Assert.AreEqual(_model.LastName, participant.LastName);
// 			Assert.AreEqual(_model.Sex, participant.Sex);
// 			Assert.AreEqual(_model.BirthDate, participant.BirthDate);
// 			Assert.AreEqual(_examination.Id, participant.ExaminationId);
//
// 			var eventTest = _eventAssertionsBuilder.Build(result.Event);
// 			eventTest.HasName("PARTICIPANT_ADD");
// 			
// 			await eventTest.HasActorIdAsync(_adminUser.ActorId);
// 			await eventTest.HasPublisherIdAsync(_space.PublisherId);
// 			await eventTest.HasPublisherIdAsync(_examination.PublisherId);
// 			await eventTest.HasPublisherIdAsync(participant.PublisherId);
// 			eventTest.HasData(participant);
// 		}
//
// 		[Test]
// 		public async Task AddParticipant_WithUser()
// 		{
// 			var result = await _participantService.AddAsync(_examination, _user, _model, _adminUser);
// 			var participant = result.Item;
// 			
// 			Assert.AreEqual(_model.Code, participant.Code);
// 			Assert.AreEqual(StringHelper.Normalize(_model.Code), participant.NormalizedCode);
// 			Assert.AreEqual(_model.FirstName, participant.FirstName);
// 			Assert.AreEqual(_model.LastName, participant.LastName);
// 			Assert.AreEqual(_model.Sex, participant.Sex);
// 			Assert.AreEqual(_model.BirthDate, participant.BirthDate);
// 			Assert.AreEqual(_examination.Id, participant.ExaminationId);
// 			Assert.AreEqual(_user.Id, participant.UserId);
//
// 			var eventTest = _eventAssertionsBuilder.Build(result.Event);
// 			eventTest.HasName("PARTICIPANT_ADD");
// 			
// 			await eventTest.HasActorIdAsync(_adminUser.ActorId);
// 			await eventTest.HasPublisherIdAsync(_space.PublisherId);
// 			await eventTest.HasPublisherIdAsync(_examination.PublisherId);
// 			await eventTest.HasPublisherIdAsync(participant.PublisherId);
// 			await eventTest.HasPublisherIdAsync(_user.PublisherId);
// 			eventTest.HasData(participant);
// 		}
// 		
// 		
// 		[Test]
// 		public async Task AddParticipant_WithStudent()
// 		{
// 			var result = await _participantService.AddAsync(_examination, _student, _model, _adminUser);
// 			var participant = result.Item;
// 			
// 			Assert.AreEqual(_model.Code, participant.Code);
// 			Assert.AreEqual(StringHelper.Normalize(_model.Code), participant.NormalizedCode);
// 			Assert.AreEqual(_model.FirstName, participant.FirstName);
// 			Assert.AreEqual(_model.LastName, participant.LastName);
// 			Assert.AreEqual(_model.Sex, participant.Sex);
// 			Assert.AreEqual(_model.BirthDate, participant.BirthDate);
// 			Assert.AreEqual(_examination.Id, participant.ExaminationId);
// 			Assert.AreEqual(_student.Id, participant.StudentId);
//
// 			var eventTest = _eventAssertionsBuilder.Build(result.Event);
// 			eventTest.HasName("PARTICIPANT_ADD");
// 			
// 			await eventTest.HasActorIdAsync(_adminUser.ActorId);
// 			await eventTest.HasPublisherIdAsync(_space.PublisherId);
// 			await eventTest.HasPublisherIdAsync(_examination.PublisherId);
// 			await eventTest.HasPublisherIdAsync(participant.PublisherId);
// 			await eventTest.HasPublisherIdAsync(_student.PublisherId);
// 			eventTest.HasData(participant);
// 		}
// 		
//
// 		[Test]
// 		public async Task TryAdd_WithUsedCode_ShouldThrow()
// 		{
// 			await _participantService.AddAsync(_examination, _model);
//
// 			var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
// 			{
// 				await _participantService.AddAsync(_examination, _model);
// 			});
// 			
// 			Assert.AreEqual("ParticipantCodeUsed", ex!.Code);
// 			Assert.AreEqual(_model.Code, ex.Params[0]);
// 		}
//
//
// 		[Test]
// 		public async Task ChangeCode()
// 		{
// 			var result = await _participantService.AddAsync(_examination, _model);
// 			var participant = result.Item;
//
// 			string code = "96fz6";
// 			var eventData = new ChangeValueData<string>(participant.Code, code);
// 			
// 			var action = await _participantService.ChangeCodeAsync(participant, code, _adminUser);
// 			await _dbContext.Entry(participant).ReloadAsync();
// 			
// 			Assert.AreEqual(code, participant.Code);
// 			Assert.AreEqual(StringHelper.Normalize(code), participant.Code);
//
// 			var eventAssert = _eventAssertionsBuilder.Build(action);
// 			await eventAssert.HasActorIdAsync(_adminUser.ActorId);
// 			await eventAssert.HasPublisherIdAsync(participant.PublisherId);
// 			await eventAssert.HasPublisherIdAsync(_examination.PublisherId);
// 			await eventAssert.HasPublisherIdAsync(_space.PublisherId);
// 			eventAssert.HasData(eventData);
// 			eventAssert.HasName("PARTICIPANT_CHANGE_CODE");
// 		}
//
// 		[Test]
// 		public async Task TryChangeCode_WithUsedCode_ShouldThrow()
// 		{
// 			var result = await _participantService.AddAsync(_examination, _model);
// 			var participant = result.Item;
//
// 			string code = "96fz6";
// 			await _participantService.ChangeCodeAsync(participant, code, _adminUser);
// 			
// 			var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
// 			{
// 				await _participantService.ChangeCodeAsync(participant, code, _adminUser);
// 			});
// 			
// 			Assert.AreEqual("ParticipantCodeUsed", ex!.Code);
// 			Assert.AreEqual(code, ex.Params[0]);
// 		}
// 		
// 		[Test]
// 		public async Task ChangeName()
// 		{
// 			var result = await _participantService.AddAsync(_examination, _model);
// 			var participant = result.Item;
//
// 			var lastName = new ChangeNameModel(participant.FirstName, participant.LastName);
// 			var changeModel = new ChangeNameModel("New First Name", "New Last Name");
// 			var eventData = new ChangeValueData<ChangeNameModel>(lastName, changeModel);
// 			var action = await _participantService.ChangeNameAsync(participant, changeModel, _adminUser);
// 			await _dbContext.Entry(participant).ReloadAsync();
// 			
// 			Assert.AreEqual(changeModel.LastName, participant.LastName);
// 			Assert.AreEqual(changeModel.FirstName, participant.FirstName);
//
// 			var eventAssert = _eventAssertionsBuilder.Build(action);
// 			await eventAssert.HasActorIdAsync(_adminUser.ActorId);
// 			await eventAssert.HasPublisherIdAsync(participant.PublisherId);
// 			await eventAssert.HasPublisherIdAsync(_examination.PublisherId);
// 			await eventAssert.HasPublisherIdAsync(_space.PublisherId);
// 			eventAssert.HasData(eventData);
// 			eventAssert.HasName("PARTICIPANT_CHANGE_NAME");
// 		}
// 		
// 		[Test]
// 		public async Task ChangeSex()
// 		{
// 			var result = await _participantService.AddAsync(_examination, _model);
// 			var participant = result.Item;
//
// 			var sex = 'F';
// 			var eventData = new ChangeValueData<char>(participant.Sex, sex);
// 			var action = await _participantService.ChangeSexAsync(participant, sex, _adminUser);
// 			await _dbContext.Entry(participant).ReloadAsync();
// 			
// 			Assert.AreEqual(sex, participant.Sex);
//
// 			var eventAssert = _eventAssertionsBuilder.Build(action);
// 			await eventAssert.HasActorIdAsync(_adminUser.ActorId);
// 			await eventAssert.HasPublisherIdAsync(participant.PublisherId);
// 			await eventAssert.HasPublisherIdAsync(_examination.PublisherId);
// 			await eventAssert.HasPublisherIdAsync(_space.PublisherId);
// 			eventAssert.HasData(eventData);
// 			eventAssert.HasName("PARTICIPANT_CHANGE_SEX");
// 		}
// 		
// 		
// 		[Test]
// 		public async Task ChangeBirthDate()
// 		{
// 			var result = await _participantService.AddAsync(_examination, _model);
// 			var participant = result.Item;
//
// 			var birthDate = new DateOnly(1990, 10, 20);
// 			var eventData = new ChangeValueData<DateOnly>(participant.BirthDate, birthDate);
// 			var action = await _participantService.ChangeBirthDateAsync(participant, birthDate, _adminUser);
// 			await _dbContext.Entry(participant).ReloadAsync();
// 			
// 			Assert.AreEqual(birthDate, participant.BirthDate);
//
// 			var eventAssert = _eventAssertionsBuilder.Build(action);
// 			await eventAssert.HasActorIdAsync(_adminUser.ActorId);
// 			await eventAssert.HasPublisherIdAsync(participant.PublisherId);
// 			await eventAssert.HasPublisherIdAsync(_examination.PublisherId);
// 			await eventAssert.HasPublisherIdAsync(_space.PublisherId);
// 			eventAssert.HasData(eventData);
// 			eventAssert.HasName("PARTICIPANT_CHANGE_BIRTHDATE");
// 		}
// 	}
// }