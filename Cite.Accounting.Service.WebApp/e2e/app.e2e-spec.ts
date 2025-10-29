import { AppPage } from '../e2e/app.po';

describe('cite.accounting.service.webapp App', () => {
	let page: AppPage;

	beforeEach(() => {
		page = new AppPage();
	});

	it('should display welcome message', () => {
		page.navigateTo();
		expect(page.getParagraphText()).toEqual('Welcome to app!');
	});
});
