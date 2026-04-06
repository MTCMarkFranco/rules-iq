/**
 * Playwright E2E Test: Loan Eligibility (Canada) — Canonical Test Persona
 *
 * This test validates the complete Rules-IQ flow:
 * 1. Navigate to the Blazor UI
 * 2. Submit the Canonical Test Persona data
 * 3. Assert compliance results (GDS rule must fail)
 * 4. Assert traceability, version info, and export
 *
 * Based on the test contract in:
 * - contracts/05-ui/ui-rule-linking-guidelines.md
 * - contracts/07-meta/meta-loan-eligibility-canada.md
 */

import { test, expect } from '@playwright/test';

test.describe('Loan Eligibility Evaluation — Canonical Test Persona', () => {

  test('should evaluate canonical persona and show GDS failure', async ({ page }) => {
    // Step 1: Navigate to the Rules-IQ Blazor UI home page
    await page.goto('/');
    await expect(page.locator('h1')).toContainText('Rules-IQ');

    // Step 2: Select the evaluation workflow
    await page.click('a[href="/evaluate"]');
    await expect(page.locator('h2')).toContainText('Loan Eligibility Evaluation');

    // Step 3: Load the Canonical Test Persona
    await page.click('button:has-text("Load Test Persona")');

    // Verify key fields are populated
    await expect(page.locator('input[type="number"]').first()).not.toHaveValue('0');

    // Step 4: Submit the evaluation
    await page.click('button:has-text("Evaluate Compliance")');

    // Wait for results to appear
    await page.waitForSelector('.card-header:has-text("Compliance Result")', { timeout: 30000 });

    // Step 5: Assert compliance percentage is displayed and is less than 100%
    const complianceBadge = page.locator('.badge:has-text("Compliant")');
    await expect(complianceBadge).toBeVisible();
    const badgeText = await complianceBadge.textContent();
    const percentage = parseFloat(badgeText!.replace('% Compliant', '').trim());
    expect(percentage).toBeLessThan(100);
    expect(percentage).toBeGreaterThan(0);

    // Step 6: Assert the compliance panel is color-coded yellow or red (NOT green/bg-success)
    const badgeClass = await complianceBadge.getAttribute('class');
    expect(badgeClass).not.toContain('bg-success');
    expect(badgeClass!.includes('bg-warning') || badgeClass!.includes('bg-danger')).toBeTruthy();

    // Step 7: Assert the MaxGrossDebtServiceRatio rule card shows Failed
    const gdsRuleCard = page.locator('.card:has-text("Max Gross Debt Service Ratio")');
    await expect(gdsRuleCard).toBeVisible();
    await expect(gdsRuleCard.locator('.badge')).toContainText('Failed');
    await expect(gdsRuleCard).toContainText('GDS ratio must not exceed 39%');

    // Step 8: Assert source document reference (OSFI B-20) is present
    // The rule card should reference the source document
    await expect(gdsRuleCard).toContainText('OSFI');

    // Step 9: Assert a policy snippet from the source document is visible
    const snippet = gdsRuleCard.locator('.snippet, blockquote, .text-muted');
    await expect(snippet).toBeVisible();
    const snippetText = await snippet.textContent();
    expect(snippetText!.length).toBeGreaterThan(0);

    // Also verify the expression is visible
    await expect(gdsRuleCard.locator('code')).toContainText('GDS');

    // Step 10: Assert Ruleset Version and Evaluation Timestamp are visible
    const versionInfo = page.locator('text=Ruleset Version');
    await expect(versionInfo).toBeVisible();
    const timestampInfo = page.locator('text=Evaluated');
    await expect(timestampInfo).toBeVisible();

    // Step 11: Assert the Rules Snapshot section is expandable
    const snapshotButton = page.locator('button:has-text("Show Rules Snapshot")');
    await expect(snapshotButton).toBeVisible();
    await snapshotButton.click();

    // The snapshot should now show a JSON pre block
    const snapshotPre = page.locator('pre');
    await expect(snapshotPre).toBeVisible();
    const snapshotText = await snapshotPre.textContent();
    expect(snapshotText).toContain('CanadianLoanEligibility');
    expect(snapshotText).toContain('MaxGrossDebtServiceRatio');
    expect(snapshotText).toContain('Failed');

    // Step 12: Assert the export button is present
    const exportButton = page.locator('button:has-text("Export JSON")');
    await expect(exportButton).toBeVisible();
    await exportButton.click();

    // After clicking export, the snapshot should be visible with valid JSON
    const exportContent = await snapshotPre.textContent();
    expect(exportContent).toContain('ComplianceScore');
    expect(exportContent).toContain('VersionFingerprint');
    expect(exportContent).toContain('RulesSnapshot');

    // Validate the JSON is parseable
    const parsed = JSON.parse(exportContent!);
    expect(parsed.ComplianceScore).toBeDefined();
    expect(parsed.ComplianceScore.CompliancePercentage).toBeLessThan(100);
    expect(parsed.ComplianceScore.FailedRules.length).toBeGreaterThan(0);
    expect(parsed.ComplianceScore.FailedRules.some(
      (r: { RuleName: string }) => r.RuleName === 'MaxGrossDebtServiceRatio'
    )).toBeTruthy();

    // Step 13: Capture screenshot of the compliance dashboard
    await page.screenshot({
      path: 'test-results/compliance-dashboard.png',
      fullPage: true
    });
  });

  test('should display all rule cards with pass/fail indicators', async ({ page }) => {
    await page.goto('/evaluate');

    // Load test persona and evaluate
    await page.click('button:has-text("Load Test Persona")');
    await page.click('button:has-text("Evaluate Compliance")');
    await page.waitForSelector('.card-header:has-text("Compliance Result")', { timeout: 30000 });

    // Count rule cards — should be 7
    const ruleCards = page.locator('.card.border-success, .card.border-danger');
    const count = await ruleCards.count();
    expect(count).toBe(7);

    // Count passed vs failed
    const passedCards = page.locator('.card.border-success');
    const failedCards = page.locator('.card.border-danger');

    const passedCount = await passedCards.count();
    const failedCount = await failedCards.count();

    // Canonical persona: GDS fails (41.5 > 39), all others should pass
    expect(failedCount).toBeGreaterThanOrEqual(1);
    expect(passedCount).toBe(count - failedCount);

    await page.screenshot({
      path: 'test-results/all-rule-cards.png',
      fullPage: true
    });
  });

  test('should navigate between home, evaluate, and rules pages', async ({ page }) => {
    // Home page
    await page.goto('/');
    await expect(page.locator('h1')).toContainText('Rules-IQ');

    // Navigate to Evaluate
    await page.click('a[href="/evaluate"]');
    await expect(page.locator('h2')).toContainText('Loan Eligibility Evaluation');

    // Navigate to Rules
    await page.click('text=Rules');
    await expect(page.locator('h2')).toContainText('Rule Browser');

    // Verify rules table has entries
    const rows = page.locator('table tbody tr');
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThan(0);

    // Navigate back home
    await page.click('text=Home');
    await expect(page.locator('h1')).toContainText('Rules-IQ');
  });
});
