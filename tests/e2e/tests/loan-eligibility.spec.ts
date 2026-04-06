/**
 * Playwright E2E Test: Loan Eligibility — Data-Agnostic
 *
 * These tests validate the structural UI flow without assuming specific
 * rule names, thresholds, or pass/fail outcomes. The rules are extracted
 * from policy PDFs by AI and may change across indexer runs.
 *
 * What we CAN assert (structural):
 * - Pages load with expected headings and layout
 * - Test persona populates the form
 * - Evaluation produces a ComplianceDashboard with a percentage, rule cards, version info
 * - Snapshot and export produce parseable JSON
 *
 * What we do NOT assert (data-dependent):
 * - Specific rule names (e.g., MaxGrossDebtServiceRatio)
 * - Specific thresholds or error messages
 * - Specific pass/fail outcomes or badge colors
 * - Exact rule count
 */

import { test, expect } from '@playwright/test';

test.describe('Loan Eligibility Evaluation', () => {

  test('should load test persona, evaluate, and display compliance results', async ({ page }) => {
    // Navigate to home and verify branding
    await page.goto('/');
    await expect(page.locator('h1')).toContainText('Rules-IQ');

    // Navigate to evaluation page
    await page.click('a[href="/evaluate"]');
    await expect(page.locator('h2')).toContainText('Loan Eligibility Evaluation');

    // Load canonical test persona and verify form is populated
    await page.click('button:has-text("Load Test Persona")');
    await expect(page.locator('input[type="number"]').first()).not.toHaveValue('');

    // Submit evaluation
    await page.click('button:has-text("Evaluate Compliance")');

    // Wait for the compliance result card to appear
    await page.waitForSelector('.card-header:has-text("Compliance Result")', { timeout: 30000 });

    // Assert compliance percentage badge is visible and contains a valid number
    const complianceBadge = page.locator('.badge:has-text("Compliant")');
    await expect(complianceBadge).toBeVisible();
    const badgeText = await complianceBadge.textContent();
    const percentage = parseFloat(badgeText!.replace('% Compliant', '').trim());
    expect(percentage).toBeGreaterThanOrEqual(0);
    expect(percentage).toBeLessThanOrEqual(100);

    // Assert the badge has one of the valid color classes (success, warning, or danger)
    const badgeClass = await complianceBadge.getAttribute('class');
    const hasValidColor =
      badgeClass!.includes('bg-success') ||
      badgeClass!.includes('bg-warning') ||
      badgeClass!.includes('bg-danger');
    expect(hasValidColor).toBeTruthy();

    // Assert the "X of Y rules passed" summary is displayed
    const summaryText = page.locator('text=/\\d+ of \\d+ rules passed/');
    await expect(summaryText).toBeVisible();

    // Assert at least one rule card is rendered (border-success or border-danger)
    const ruleCards = page.locator('.card.border-success, .card.border-danger');
    const cardCount = await ruleCards.count();
    expect(cardCount).toBeGreaterThan(0);

    // Assert each rule card has a name, expression, and a Passed/Failed badge
    for (let i = 0; i < cardCount; i++) {
      const card = ruleCards.nth(i);
      await expect(card.locator('strong')).toBeVisible();          // rule name
      await expect(card.locator('code')).toBeVisible();            // expression
      const badge = card.locator('.badge');
      const text = await badge.textContent();
      expect(text!.includes('Passed') || text!.includes('Failed')).toBeTruthy();
    }

    // Assert failed cards show an error message
    const failedCards = page.locator('.card.border-danger');
    const failedCount = await failedCards.count();
    for (let i = 0; i < failedCount; i++) {
      const errorMsg = failedCards.nth(i).locator('.text-danger');
      await expect(errorMsg).toBeVisible();
    }

    // Assert Ruleset Version and Evaluation Timestamp are visible
    await expect(page.locator('text=Ruleset Version')).toBeVisible();
    await expect(page.locator('text=Evaluated')).toBeVisible();

    // Assert the Rules Snapshot section is expandable and contains JSON
    const snapshotButton = page.locator('button:has-text("Show Rules Snapshot")');
    await expect(snapshotButton).toBeVisible();
    await snapshotButton.click();

    const snapshotPre = page.locator('pre');
    await expect(snapshotPre).toBeVisible();
    const snapshotText = await snapshotPre.textContent();
    // Validate it's parseable JSON without checking specific field values
    const snapshotJson = JSON.parse(snapshotText!);
    expect(snapshotJson).toBeDefined();

    // Assert Export JSON button works and produces valid structured output
    const exportButton = page.locator('button:has-text("Export JSON")');
    await expect(exportButton).toBeVisible();
    await exportButton.click();

    const exportContent = await snapshotPre.textContent();
    const parsed = JSON.parse(exportContent!);
    expect(parsed.ComplianceScore).toBeDefined();
    expect(parsed.ComplianceScore.TotalRulesEvaluated).toBeGreaterThan(0);
    expect(parsed.ComplianceScore.CompliancePercentage).toBeGreaterThanOrEqual(0);
    expect(parsed.ComplianceScore.CompliancePercentage).toBeLessThanOrEqual(100);
    expect(parsed.VersionFingerprint).toBeDefined();
    expect(parsed.RulesSnapshot).toBeDefined();

    await page.screenshot({ path: 'test-results/compliance-dashboard.png', fullPage: true });
  });

  test('should display rule cards with consistent pass/fail counts', async ({ page }) => {
    await page.goto('/evaluate');

    await page.click('button:has-text("Load Test Persona")');
    await page.click('button:has-text("Evaluate Compliance")');
    await page.waitForSelector('.card-header:has-text("Compliance Result")', { timeout: 30000 });

    // Count rule cards — at least 1 must exist
    const ruleCards = page.locator('.card.border-success, .card.border-danger');
    const totalCards = await ruleCards.count();
    expect(totalCards).toBeGreaterThan(0);

    // passed + failed should equal total
    const passedCards = page.locator('.card.border-success');
    const failedCards = page.locator('.card.border-danger');
    const passedCount = await passedCards.count();
    const failedCount = await failedCards.count();
    expect(passedCount + failedCount).toBe(totalCards);

    // The "X of Y rules passed" summary should match the card counts
    const summaryText = await page.locator('text=/\\d+ of \\d+ rules passed/').textContent();
    const match = summaryText!.match(/(\d+) of (\d+) rules passed/);
    expect(match).not.toBeNull();
    expect(parseInt(match![1])).toBe(passedCount);
    expect(parseInt(match![2])).toBe(totalCards);

    await page.screenshot({ path: 'test-results/all-rule-cards.png', fullPage: true });
  });

  test('should navigate between home, evaluate, and rules pages', async ({ page }) => {
    // Home page
    await page.goto('/');
    await expect(page.locator('h1')).toContainText('Rules-IQ');

    // Navigate to Evaluate via the card link
    await page.click('a[href="/evaluate"]');
    await expect(page.locator('h2')).toContainText('Loan Eligibility Evaluation');

    // Navigate to Rules via the sidebar nav link
    await page.click('a[href="rules"]');
    await expect(page.locator('h2')).toContainText('Rule Browser');

    // Verify rules table has entries (wait for async load from search index)
    await page.waitForSelector('table tbody tr', { timeout: 30000 });
    const rows = page.locator('table tbody tr');
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThan(0);

    // Navigate back home via sidebar
    await page.click('a[href=""]');
    await expect(page.locator('h1')).toContainText('Rules-IQ');
  });
});
