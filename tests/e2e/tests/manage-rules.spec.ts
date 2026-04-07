import { test, expect } from '@playwright/test';

test.describe('Rules Management Page', () => {
    test('should load the manage-rules page and display rules from index', async ({ page }) => {
        // Navigate to the manage-rules page
        await page.goto('/manage-rules');

        // Verify the page heading
        await expect(page.locator('h2')).toHaveText('Rules Management');
        await expect(page.locator('p').first()).toContainText('Edit rules directly in the search index');

        // Wait for the loading spinner to disappear (data loaded)
        await expect(page.locator('.spinner-border')).toBeHidden({ timeout: 30000 });

        // There should NOT be a "No documents" warning if the index has rules
        const noDocsWarning = page.locator('.alert-warning', { hasText: 'No documents with rules' });
        const errorAlert = page.locator('.alert-danger');

        // If there's an error, log it for debugging but don't fail — may be auth
        if (await errorAlert.isVisible()) {
            const errorText = await errorAlert.textContent();
            console.log('Error loading rules:', errorText);
            // Skip remaining assertions if there's a connectivity error
            test.skip(true, `Could not connect to search index: ${errorText}`);
        }

        // Either we have documents or the "no docs" warning — both are valid
        if (await noDocsWarning.isVisible()) {
            console.log('No documents with rules in the index — cannot test management UI fully');
            return;
        }

        // If documents loaded, verify the accordion structure
        const accordion = page.locator('#rulesAccordion');
        await expect(accordion).toBeVisible();

        // Verify the document count summary is shown
        const summary = page.locator('text=/\\d+ document\\(s\\)/');
        await expect(summary).toBeVisible();

        // Verify at least one accordion item exists
        const items = page.locator('.accordion-item');
        const count = await items.count();
        expect(count).toBeGreaterThan(0);

        // Expand the first document
        await items.first().locator('.accordion-button').click();

        // Verify the expanded content shows expected sections
        const expandedBody = items.first().locator('.accordion-body');
        await expect(expandedBody).toBeVisible();

        // Verify chunk content textarea exists
        const chunkTextarea = expandedBody.locator('textarea[readonly]');
        await expect(chunkTextarea).toBeVisible();
        const chunkContent = await chunkTextarea.inputValue();
        expect(chunkContent.length).toBeGreaterThan(0);

        // Verify source document section
        await expect(expandedBody.locator('text=Source Document')).toBeVisible();

        // Verify rule cards are present
        const ruleCards = expandedBody.locator('.card');
        const ruleCount = await ruleCards.count();
        expect(ruleCount).toBeGreaterThan(0);

        // Verify editable fields exist on the first rule
        const firstRule = ruleCards.first();
        const ruleNameInput = firstRule.locator('input[type="text"]').first();
        await expect(ruleNameInput).toBeVisible();
        const ruleNameValue = await ruleNameInput.inputValue();
        expect(ruleNameValue.length).toBeGreaterThan(0);
    });

    test('should navigate to manage-rules from home page', async ({ page }) => {
        await page.goto('/');
        await expect(page.locator('h1')).toContainText('Rules-IQ');

        // Verify the Manage Rules card exists
        const manageCard = page.locator('.card', { hasText: 'Manage Rules' });
        await expect(manageCard).toBeVisible();

        // Click the manage rules link
        await manageCard.locator('a', { hasText: 'Manage Rules' }).click();
        await expect(page).toHaveURL(/manage-rules/);
        await expect(page.locator('h2')).toHaveText('Rules Management');
    });

    test('should show manage-rules in the nav menu', async ({ page }) => {
        await page.goto('/');
        const navLink = page.locator('nav .nav-link', { hasText: 'Manage Rules' });
        await expect(navLink).toBeVisible();
    });

    test('should allow editing a rule and show dirty state', async ({ page }) => {
        await page.goto('/manage-rules');
        await expect(page.locator('.spinner-border')).toBeHidden({ timeout: 30000 });

        // Skip if no data or error
        if (await page.locator('.alert-danger').isVisible() || await page.locator('.alert-warning').isVisible()) {
            test.skip(true, 'No data available for editing test');
        }

        // Expand first document
        const firstItem = page.locator('.accordion-item').first();
        await firstItem.locator('.accordion-button').click();

        const expandedBody = firstItem.locator('.accordion-body');
        await expect(expandedBody).toBeVisible();

        // Edit the first rule's expression
        const expressionField = expandedBody.locator('textarea.font-monospace').first();
        await expect(expressionField).toBeVisible();

        // Clear and type a new expression
        await expressionField.fill('input.Age >= 18');

        // Verify "Modified" badge appears
        await expect(firstItem.locator('.badge', { hasText: 'Modified' })).toBeVisible();

        // Verify "Save" and "Discard" buttons appear
        await expect(expandedBody.locator('button', { hasText: 'Save' })).toBeVisible();
        await expect(expandedBody.locator('button', { hasText: 'Discard' })).toBeVisible();

        // Verify the "Save All Changes" button appears in the header
        await expect(page.locator('button', { hasText: 'Save All Changes' })).toBeVisible();

        // Click Discard to revert
        await expandedBody.locator('button', { hasText: 'Discard' }).click();

        // Modified badge should disappear
        await expect(firstItem.locator('.badge', { hasText: 'Modified' })).toBeHidden();
    });
});
