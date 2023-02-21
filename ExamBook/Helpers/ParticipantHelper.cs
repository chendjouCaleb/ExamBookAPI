using System;
using ExamBook.Entities;

namespace ExamBook.Helpers
{
    public class ParticipantHelper
    {
        public static void ThrowParticipantNotFound(Examination examination, string rId)
        {
            var m = $"Participant with rId: {rId} not found in examination: {examination.Name}.";
            throw new InvalidOperationException(m);
        }
        
        public static void ThrowDuplicateRId(Examination examination, string rId)
        {
            var m = $"The rId: {rId} is already used  in examination: {examination.Name}.";
            throw new InvalidOperationException(m);
        }
        
        public static void ThrowDuplicateParticipantSpeciality(ExaminationSpeciality speciality, Participant participant)
        {
            var m = $"The participant: {participant.RId} is already found in speciality: {speciality.Name} in" +
                    $" examination: {speciality.Examination.Name}.";
            throw new InvalidOperationException(m);
        }
    }
}