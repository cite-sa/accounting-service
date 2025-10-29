using Cite.Accounting.Service.Common.Validation;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Model
{
	public class StorageFile
	{
		public Guid? Id { get; set; }
		public String FileRef { get; set; }
		public String Name { get; set; }
		public String Extension { get; set; }
		public String FullName { get; set; }
		public String MimeType { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? PurgeAt { get; set; }
		public DateTime? PurgedAt { get; set; }
		public String Hash { get; set; }
	}

	public class StorageFilePersist
	{
		public String Name { get; set; }
		public String Extension { get; set; }
		public String MimeType { get; set; }
		public TimeSpan? Lifetime { get; set; }

		public class Validator : BaseValidator<StorageFilePersist>
		{
			private static readonly int NameMaxLenth = typeof(Data.StorageFile).MaxLengthOf(nameof(Data.StorageFile.Name));
			private static readonly int ExtensionMaxLenth = typeof(Data.StorageFile).MaxLengthOf(nameof(Data.StorageFile.Extension));
			private static readonly int MimeTypeMaxLenth = typeof(Data.StorageFile).MaxLengthOf(nameof(Data.StorageFile.MimeType));

			public Validator(
				IConventionService conventionService,
				IStringLocalizer<Resources.MySharedResources> localizer,
				ValidatorFactory validatorFactory,
				ILogger<Validator> logger,
				ErrorThesaurus errors) : base(conventionService, validatorFactory, logger, errors)
			{
				this._localizer = localizer;
			}

			private readonly IStringLocalizer<Resources.MySharedResources> _localizer;

			protected override IEnumerable<ISpecification> Specifications(StorageFilePersist item)
			{
				return new ISpecification[]{
                    //name must be set
                    this.Spec()
						.Must(() => !this.IsEmpty(item.Name))
						.FailOn(nameof(StorageFilePersist.Name)).FailWith(this._localizer["Validation_Required", nameof(StorageFilePersist.Name)]),
                    //name max length
                    this.Spec()
						.If(() => !this.IsEmpty(item.Name))
						.Must(() => item.Name.Length <= Validator.NameMaxLenth)
						.FailOn(nameof(StorageFilePersist.Name)).FailWith(this._localizer["Validation_Required", nameof(StorageFilePersist.Name)]),
                    //extension must be set
                    this.Spec()
						.Must(() => !this.IsEmpty(item.Extension))
						.FailOn(nameof(StorageFilePersist.Extension)).FailWith(this._localizer["Validation_Required", nameof(StorageFilePersist.Extension)]),
                    //extension max length
                    this.Spec()
						.If(() => !this.IsEmpty(item.Extension))
						.Must(() => item.Extension.Length <= Validator.ExtensionMaxLenth)
						.FailOn(nameof(StorageFilePersist.Extension)).FailWith(this._localizer["Validation_Required", nameof(StorageFilePersist.Extension)]),
                    //mime type must be set
                    this.Spec()
						.Must(() => !this.IsEmpty(item.MimeType))
						.FailOn(nameof(StorageFilePersist.MimeType)).FailWith(this._localizer["Validation_Required", nameof(StorageFilePersist.MimeType)]),
                    //mime type max length
                    this.Spec()
						.If(() => !this.IsEmpty(item.MimeType))
						.Must(() => item.MimeType.Length <= Validator.MimeTypeMaxLenth)
						.FailOn(nameof(StorageFilePersist.MimeType)).FailWith(this._localizer["Validation_Required", nameof(StorageFilePersist.MimeType)])
				};
			}
		}
	}
}
