import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { AuthTokenInterceptor } from '@common/http/interceptors/auth-token.interceptor';
import { JsonInterceptor } from '@common/http/interceptors/json.interceptor';
import { LocaleInterceptor } from '@common/http/interceptors/locale.interceptor';
import { ProgressIndicationInterceptor } from '@common/http/interceptors/progress-indication.interceptor';
import { RequestTimingInterceptor } from '@common/http/interceptors/request-timing.interceptor';
import { UnauthorizedResponseInterceptor } from '@common/http/interceptors/unauthorized-response.interceptor';
import { UserConsentInterceptor } from '@common/http/interceptors/user-consent.interceptor';

@NgModule({
	imports: [
	],
	declarations: [
	],
	providers: [
		{
			provide: HTTP_INTERCEPTORS,
			useClass: AuthTokenInterceptor,
			multi: true,
		},
		{
			provide: HTTP_INTERCEPTORS,
			useClass: JsonInterceptor,
			multi: true,
		},
		{
			provide: HTTP_INTERCEPTORS,
			useClass: LocaleInterceptor,
			multi: true,
		},
		{
			provide: HTTP_INTERCEPTORS,
			useClass: UnauthorizedResponseInterceptor,
			multi: true,
		},
		{
			provide: HTTP_INTERCEPTORS,
			useClass: RequestTimingInterceptor,
			multi: true,
		},
		{
			provide: HTTP_INTERCEPTORS,
			useClass: ProgressIndicationInterceptor,
			multi: true,
		},
		{
			provide: HTTP_INTERCEPTORS,
			useClass: UserConsentInterceptor,
			multi: true,
		},
	]
})
export class CommonHttpModule { }
