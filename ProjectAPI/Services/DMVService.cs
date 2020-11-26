﻿using Newtonsoft.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Model.Repositories.Interfaces;
using ProjectAPI.Services.Interfaces;
using Model.Models;
using System.Data;

namespace ProjectAPI.Services
{
    public class DMVService : IDMVService
    {
        private IDMVRepository _dmvRepository;
        private IAccountRepository _accountRepository;
        private IMailService _mailService;
        private readonly IMapper _mapper;
        private ILogger _logger;

        public DMVService(IMapper mapper, IDMVRepository dmvRepository, ILogger<DMVService> logger, IAccountRepository accountRepository, IMailService mailService)
        {
            _dmvRepository = dmvRepository;
            _mapper = mapper;
            _logger = logger;
            _accountRepository = accountRepository;
            _mailService = mailService;
        }


        public async Task GetLicenses()
        {
            try
            {
                List<DMV> dmv = new List<DMV>();
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync("https://localhost:44396/api/dmv/xfnxXuOk72NvzE3t007"))
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        dmv = JsonConvert.DeserializeObject<List<DMV>>(apiResponse);
                        _logger.LogInformation("DMV service data retrieved");
                    }
                }
                _dmvRepository.SaveDMVList(dmv);

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured: " + ex.Message);
            }
        }

        public List<DMVDto> GetDMVDtos()
        {
            return _mapper.Map<List<DMVDto>>(_dmvRepository.GetDMV());
        }

        public bool validateLicenseWithDMV(string licenseId)
        {
            return _dmvRepository.ValidIdExists(licenseId);
        }

        public async Task<bool> DMVNotification(int accountId, string licenseId)
        {
            List<DMV> dmv = await Task.FromResult(_dmvRepository.GetOffense(licenseId));
            if(dmv.Count == 0)
            {
                return false;
            }

            SendDMVNotification(accountId, dmv.FirstOrDefault());
            return true;
        }



        private void SendDMVNotification(int accountId, DMV dmv)
        {
            Account account = _accountRepository.GetAccountById(accountId);
            account.DecryptModel();
            _mailService.SendDMVNotification(account.firstName + " " + account.lastName,account.drivingLicense, dmv.type, dmv.offenseDate.ToString(), dmv.id, dmv.drivingLicense);
        }
    }
}
