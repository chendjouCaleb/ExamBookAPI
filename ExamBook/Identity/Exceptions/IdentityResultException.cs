using System;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace ExamBook.Identity.Exceptions
{
	public class IdentityResultException:ApplicationException
	{
		public IdentityResult Result { get; }

		public static void ThrowIfError(IdentityResult result)
		{
			if (!result.Succeeded)
			{
				throw new IdentityResultException(result);
			}
		}


		public IdentityResultException(IdentityResult result):base(JsonConvert.SerializeObject(result))
		{
			Result = result;
		}
	}
}