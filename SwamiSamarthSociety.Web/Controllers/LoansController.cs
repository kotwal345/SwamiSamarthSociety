using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwamiSamarthSociety.Data.Entities;
using SwamiSamarthSociety.Services;
using SwamiSamarthSociety.Web.Models;

namespace SwamiSamarthSociety.Web.Controllers
{
    [Authorize]
    public class LoansController : Controller
    {
        private readonly ILoanService _loanService;
        public LoansController(ILoanService loanService) => _loanService = loanService;

        public async Task<IActionResult> Index(string? status = null)
        {
            var loans = await _loanService.GetAllAsync();
            if (!string.IsNullOrEmpty(status))
                loans = loans.Where(l => l.Status == status).ToList();
            ViewBag.StatusFilter = status;
            return View(loans);
        }

        public async Task<IActionResult> Details(int id)
        {
            var loan = await _loanService.GetByIdAsync(id);
            if (loan is null) return NotFound();
            return View(loan);
        }

        [Authorize(Roles = AppRoles.Chairman)]
        public async Task<IActionResult> Create()
        {
            var vm = new LoanCreateViewModel
            {
                BorrowerCandidates = await _loanService.GetBorrowerCandidatesAsync(),
                GuarantorsRequired = await _loanService.GetGuarantorsRequiredAsync()
            };
            vm.GuarantorCandidates = vm.BorrowerCandidates; // refined once a borrower is picked (client-side excludes self)
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Chairman)]
        public async Task<IActionResult> Create(LoanCreateViewModel vm)
        {
            var errors = ModelState.IsValid
                ? await _loanService.ValidateNewLoanAsync(vm.MemberId, vm.OriginalLoanAmount, vm.GuarantorMemberIds)
                : new List<string>();

            foreach (var error in errors)
                ModelState.AddModelError(string.Empty, error);

            if (!ModelState.IsValid)
            {
                vm.BorrowerCandidates = await _loanService.GetBorrowerCandidatesAsync();
                vm.GuarantorCandidates = await _loanService.GetGuarantorCandidatesAsync(vm.MemberId);
                vm.GuarantorsRequired = await _loanService.GetGuarantorsRequiredAsync();
                return View(vm);
            }

            var loan = new Loan
            {
                MemberId = vm.MemberId,
                OriginalLoanAmount = vm.OriginalLoanAmount,
                LoanDate = vm.LoanDate,
                InterestRate = vm.InterestRate,
                Remarks = vm.Remarks,
                ApprovedBy = User.Identity?.Name,
                ApprovedDate = DateTime.UtcNow,
                DisbursementDate = vm.LoanDate
            };

            var created = await _loanService.CreateAsync(loan, vm.GuarantorMemberIds);
            TempData["Success"] = $"Loan {created.LoanNumber} of ₹{created.OriginalLoanAmount:N0} created.";
            return RedirectToAction(nameof(Details), new { id = created.LoanId });
        }
    }
}
