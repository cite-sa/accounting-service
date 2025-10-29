using System;

namespace Cite.Accounting.Service.Service.WhatYouKnowAboutMe
{
	public class ExtractedUserInfo
	{
		public class ContactInfo
		{
			public String Type { get; set; }
			public String Value { get; set; }
			public DateTime CreatedAt { get; set; }
		}

		public class ProfileInfo
		{
			public String Timezone { get; set; }
			public String Culture { get; set; }
			public String Language { get; set; }
		}

		public ProfileInfo Profile { get; set; }
	}
}
