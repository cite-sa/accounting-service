using Cite.Accounting.Service.Common.Validation.Extensions;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Common.Types;
using Cite.Tools.Exception;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Cite.Tools.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TimeZoneConverter;

namespace Cite.Accounting.Service.Common.Validation
{
	public abstract class BaseValidator<T> : AbstractValidator<T>
	{
		protected IConventionService _conventionService;
		protected ValidatorFactory _validatorFactory;
		protected ILogger _logger;
		protected ErrorThesaurus _errors;

		public BaseValidator(
			IConventionService conventionService,
			ValidatorFactory validatorFactory,
			ILogger logger,
			ErrorThesaurus errors)
		{
			this._conventionService = conventionService;
			this._validatorFactory = validatorFactory;
			this._logger = logger;
			this._errors = errors;
		}

		public override void ValidateForce(Object item)
		{
			this._logger.Trace("validating item");
			this.Validate(item);
			if (this.Result.IsValid) { this._logger.Trace("valid"); return; }
			List<Pair<String, List<String>>> errors = this.FlattenValidationResult().Select(x => new Pair<String, List<String>>(x.Key, x.Value)).ToList();
			this._logger.Debug(new DataLogEntry("validation failed", errors));
			this._logger.Trace("throwing");
			throw new MyValidationException(this._errors.ModelValidation.Code, errors);
		}

		protected Boolean IsValidId(int? id)
		{
			return this._conventionService.IsValidId(id);
		}

		protected Boolean IsValidId(String id)
		{
			if (!int.TryParse(id, out int tmp)) return false;
			return this._conventionService.IsValidId(tmp);
		}

		protected Boolean IsValidGuid(Guid? guid)
		{
			return this._conventionService.IsValidGuid(guid);
		}

		protected Boolean IsValidHash(String hash)
		{
			return this._conventionService.IsValidHash(hash);
		}

		protected Boolean IsEmpty(String value)
		{
			return String.IsNullOrEmpty(value);
		}

		protected Boolean IsValidEmail(String value)
		{
			return value.IsValidEmail();
		}

		protected Boolean IsValidE164Phone(String value)
		{
			return value.IsValidE164Phone();
		}

		protected Boolean IsValidTimezone(String timezone)
		{
			try
			{
				TZConvert.GetTimeZoneInfo(timezone);
				return true;
			}
			catch (System.Exception)
			{
				return false;
			}
		}

		protected Boolean IsValidCulture(String culture)
		{
			try
			{
				CultureInfo.GetCultureInfo(culture);
				return true;
			}
			catch (System.Exception)
			{
				return false;
			}
		}

		protected Boolean HasValue<S>(Nullable<S> value) where S : struct
		{
			return value.HasValue;
		}

		protected Boolean LessEqual(String value, int size)
		{
			return value.Length <= size;
		}
	}
}
