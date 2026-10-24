using System.Diagnostics;
using System.Net.Http;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BankAccountPlaywrightTests;

[TestClass]
[DoNotParallelize]
public class BankAccountUiTests : PageTest
{
    private const string BaseUrl = "http://127.0.0.1:5187";
    private static Process? server;

    [ClassInitialize]
    public static async Task StartServer(TestContext context)
    {
        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "BankAccountWeb", "BankAccountWeb.csproj"));
        var start = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --no-launch-profile --project \"{projectPath}\" --urls {BaseUrl}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        server = Process.Start(start) ?? throw new InvalidOperationException("Cannot start web server.");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        for (int attempt = 0; attempt < 60; attempt++)
        {
            if (server.HasExited) throw new InvalidOperationException("Web server exited unexpectedly.");
            try { using var response = await http.GetAsync(BaseUrl + "/api/balance"); if (response.IsSuccessStatusCode) return; }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            await Task.Delay(500);
        }
        throw new TimeoutException("Web server did not start within 30 seconds. Check port 5187.");
    }

    [ClassCleanup]
    public static void StopServer()
    {
        if (server is { HasExited: false }) server.Kill(entireProcessTree: true);
        server?.Dispose();
    }

    [TestInitialize]
    public async Task ResetAccount()
    {
        using var http = new HttpClient();
        using var response = await http.PostAsync(BaseUrl + "/api/reset", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task OpenApp()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(Page.GetByTestId("balance")).ToHaveTextAsync("100.00 PLN");
    }

    [TestMethod]
    [TestCategory("Smoke")]
    public async Task Homepage_DisplaysInitialBalance()
    {
        await OpenApp();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Bank Account Manager" })).ToBeVisibleAsync();
    }

    [TestMethod]
    [TestCategory("Smoke")]
    public async Task Deposit_ValidAmount_IncreasesBalance()
    {
        await OpenApp();
        await Page.GetByLabel("Amount (PLN)").FillAsync("50");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Deposit" }).ClickAsync();
        await Expect(Page.GetByTestId("balance")).ToHaveTextAsync("150.00 PLN");
        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Deposit successful.");
    }

    [TestMethod]
    public async Task Withdraw_ValidAmount_DecreasesBalance()
    {
        await OpenApp();
        await Page.GetByLabel("Amount (PLN)").FillAsync("40");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Withdraw" }).ClickAsync();
        await Expect(Page.GetByTestId("balance")).ToHaveTextAsync("60.00 PLN");
    }

    [TestMethod]
    public async Task Withdraw_TooMuch_ShowsErrorAndKeepsBalance()
    {
        await OpenApp();
        await Page.GetByLabel("Amount (PLN)").FillAsync("150");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Withdraw" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Insufficient funds.");
        await Expect(Page.GetByTestId("balance")).ToHaveTextAsync("100.00 PLN");
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("-5")]
    public async Task Deposit_NonPositiveAmount_ShowsValidation(string invalidAmount)
    {
        await OpenApp();
        await Page.GetByLabel("Amount (PLN)").FillAsync(invalidAmount);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Deposit" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Amount must be positive.");
        await Expect(Page.GetByTestId("balance")).ToHaveTextAsync("100.00 PLN");
    }

    [TestMethod]
    public async Task Balance_AfterReload_RemainsUpdated()
    {
        await OpenApp();
        await Page.GetByLabel("Amount (PLN)").FillAsync("25");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Deposit" }).ClickAsync();
        await Expect(Page.GetByTestId("balance")).ToHaveTextAsync("125.00 PLN");
        await Page.ReloadAsync();
        await Expect(Page.GetByTestId("balance")).ToHaveTextAsync("125.00 PLN");
    }
}
