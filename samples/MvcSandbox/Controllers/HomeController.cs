// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers
{
    public class HomeController : Controller
    {
        [ModelBinder]
        public string Id { get; set; }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RegularEnum(CarType carType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Content("CarType: " + carType.ToString());
        }

        public IActionResult FlagsEnum(CarOptions carOptions)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Content("CarOptions: " + carOptions.ToString());
        }
    }

    public enum CarType
    {
        Coupe,
        Sedan,
        Hatchback
    }

    [Flags]
    public enum CarOptions
    {
        // The flag for SunRoof is 0001.
        SunRoof = 0x01,
        // The flag for Spoiler is 0010.
        Spoiler = 0x02,
        // The flag for FogLights is 0100.
        FogLights = 0x04,
        // The flag for TintedWindows is 1000.
        TintedWindows = 0x08,
    }
}
