using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ASC.Web.Api.Tests.Infrastructure
{
    class ApiTestsData
    {
        public static IEnumerable<TestCaseData> UserForWizard()
        {
            yield return new TestCaseData("testuser@onlyoffice.com", "00000000", "en-US", "UTC", "", "", false);
        }
        
        public static IEnumerable<TestCaseData> WebStudioSettingsData()
        {
            yield return new TestCaseData(1, Guid.Parse("00000000-0000-0000-0000-000000000000"), "{\"Completed\":false}");
        }

    }
}
