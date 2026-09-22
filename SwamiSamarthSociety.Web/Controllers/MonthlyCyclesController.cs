using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwamiSamarthSociety.Services;
using SwamiSamarthSociety.Web.Models;
using SwamiSamarthSociety.Web.Reporting;

namespace SwamiSamarthSociety.Web.Controllers
{
    [Authorize]
    public class MonthlyCyclesController : Controller
    {
        private readonly IMonthlyCycleService _cycleService;
        private readonly IPaymentCollectionService _paymentCollectionService;
        private readonly IPaymentConfirmationService _paymentConfirmationService;

        public MonthlyCyclesController(
            IMonthlyCycleService cycleService,
            IPaymentCollectionService paymentCollectionService,
            IPaymentConfirmationService paymentConfirmationService)
        {
            _cycleService = cycleService;
            _paymentCollectionService = paymentCollectionService;
            _paymentConfirmationService = paymentConfirmationService;
        }

        public async Task<IActionResult> Index()
        {
            var cycles = await _cycleService.GetAllAsync();
            ViewBag.HasOpenCycle = cycles.Any(c => c.Status == "Open");
            return View(cycles);
        }

        public async Task<IActionResult> Details(int id)
        {
            var cycle = await _cycleService.GetByIdAsync(id);
            if (cycle is null) return NotFound();

            var vm = new MonthlyCycleDetailsViewModel
            {
                Cycle = cycle,
                Rows = await _cycleService.GetCollectionSheetRowsAsync(id)
            };
            return View(vm);
        }

        public async Task<IActionResult> ExportExcel(int id)
        {
            var cycle = await _cycleService.GetByIdAsync(id);
            if (cycle is null) return NotFound();

            var rows = await _cycleService.GetCollectionSheetRowsAsync(id);
            var bytes = MonthlyCollectionSheetExcelExporter.Export(cycle, rows);
            var fileName = $"SwamiSamarthSociety_{cycle.Year}-{cycle.Month:D2}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> ExportPdf(int id)
        {
            var cycle = await _cycleService.GetByIdAsync(id);
            if (cycle is null) return NotFound();

            var rows = await _cycleService.GetCollectionSheetRowsAsync(id);
            var bytes = MonthlyCollectionSheetPdfExporter.Export(cycle, rows);
            var fileName = $"SwamiSamarthSociety_{cycle.Year}-{cycle.Month:D2}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        [Authorize(Roles = AppRoles.Chairman)]
        public async Task<IActionResult> OpenNextCycle()
        {
            if (await _cycleService.GetOpenCycleAsync() is { } openCycle)
            {
                TempData["Error"] = $"An open cycle already exists ({openCycle.Month:D2}/{openCycle.Year}). Close it before opening a new one.";
                return RedirectToAction(nameof(Index));
            }

            var (year, month) = await _cycleService.GetNextCyclePeriodAsync();
            var cycles = await _cycleService.GetAllAsync();
            var previousClosingBalance = cycles.FirstOrDefault(c => c.Status == "Closed")?.ClosingBankBalance ?? 0m;

            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.OpeningBankBalance = previousClosingBalance;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Chairman)]
        public async Task<IActionResult> OpenNextCycle(decimal openingBankBalance)
        {
            try
            {
                var cycle = await _cycleService.OpenNextCycleAsync(openingBankBalance);
                TempData["Success"] = $"{cycle.Month:D2}/{cycle.Year} cycle opened.";
                return RedirectToAction(nameof(Details), new { id = cycle.MonthlyCycleId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Chairman)]
        public async Task<IActionResult> Collect(
            int cycleId, int memberId, decimal shareAmountPaid, decimal? loanAmountPaid,
            DateTime paymentDate, string? paymentMethod, string? receiptNumber)
        {
            var result = await _paymentCollectionService.RecordPaymentAsync(
                cycleId, memberId, shareAmountPaid, loanAmountPaid, paymentDate,
                paymentMethod, receiptNumber, User.Identity?.Name);

            if (result.Success)
            {
                var totalPaid = shareAmountPaid + (loanAmountPaid ?? 0);
                await _paymentConfirmationService.NotifyAsync(memberId, totalPaid, paymentDate);
            }

            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Payment recorded." : result.Error;
            return RedirectToAction(nameof(Details), new { id = cycleId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Chairman)]
        public async Task<IActionResult> RecalculateDue(int id)
        {
            var changed = await _cycleService.RecalculateOpenCycleDueAsync();
            TempData["Success"] = changed > 0
                ? $"Penalty/arrears re-applied to {changed} member(s)."
                : "No changes -- every unpaid installment already reflects the current rule.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Chairman)]
        public async Task<IActionResult> Close(int id, decimal closingBankBalance)
        {
            try
            {
                await _cycleService.CloseCycleAsync(id, closingBankBalance);
                TempData["Success"] = "Cycle closed.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
