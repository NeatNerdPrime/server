﻿using System.Threading.Tasks;
using Bit.Core;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Portal.Controllers
{
    [Authorize]
    public class SsoController : Controller
    {
        private readonly ISsoConfigRepository _ssoConfigRepository;
        private readonly EnterprisePortalCurrentContext _enterprisePortalCurrentContext;
        private readonly II18nService _i18nService;
        private readonly GlobalSettings _globalSettings;

        public SsoController(
            ISsoConfigRepository ssoConfigRepository,
            EnterprisePortalCurrentContext enterprisePortalCurrentContext,
            II18nService i18nService,
            GlobalSettings globalSettings)
        {
            _ssoConfigRepository = ssoConfigRepository;
            _enterprisePortalCurrentContext = enterprisePortalCurrentContext;
            _i18nService = i18nService;
            _globalSettings = globalSettings;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orgId = _enterprisePortalCurrentContext.SelectedOrganizationId;
            if (orgId == null)
            {
                return Redirect("~/");
            }

            if (!_enterprisePortalCurrentContext.SelectedOrganizationDetails.UseSso ||
                !_enterprisePortalCurrentContext.AdminForSelectedOrganization)
            {
                return Redirect("~/");
            }

            var ssoConfig = await _ssoConfigRepository.GetByOrganizationIdAsync(orgId.Value);
            var model = new SsoConfigEditViewModel(ssoConfig, _i18nService, _globalSettings);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SsoConfigEditViewModel model)
        {
            var orgId = _enterprisePortalCurrentContext.SelectedOrganizationId;
            if (orgId == null)
            {
                return Redirect("~/");
            }

            if (!_enterprisePortalCurrentContext.SelectedOrganizationDetails.UseSso ||
                !_enterprisePortalCurrentContext.AdminForSelectedOrganization)
            {
                return Redirect("~/");
            }

            model.BuildLists(_i18nService);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ssoConfig = await _ssoConfigRepository.GetByOrganizationIdAsync(orgId.Value);
            if (ssoConfig == null)
            {
                ssoConfig = model.ToSsoConfig();
                ssoConfig.OrganizationId = orgId.GetValueOrDefault();
                await _ssoConfigRepository.CreateAsync(ssoConfig);
            }
            else
            {
                ssoConfig = model.ToSsoConfig(ssoConfig);
                await _ssoConfigRepository.ReplaceAsync(ssoConfig);
            }

            return View(model);
        }
    }
}
