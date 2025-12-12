/*
 * Program:         Project2_Wordle
 * File:            IDailyWordService.cs
 * Date:            March 13, 2025
 * Author:          Jazz Leibur, 0690432, Section 2 & Alec Lahey, 1056146, Section 2
 * Description:     WCF Service Contract for DailyWord service.
 */

using System.ServiceModel;
using System.Runtime.Serialization;

namespace WordServer.Services
{
    [ServiceContract]
    public interface IDailyWordService
    {
        [OperationContract]
        DailyWordResponse GetWord(GetWordRequest request);

        [OperationContract]
        ValidationResponse ValidateWord(SubmittedWord request);
    }

    [DataContract]
    public class GetWordRequest
    {
        // Empty request - no parameters needed
    }

    [DataContract]
    public class DailyWordResponse
    {
        [DataMember]
        public string Word { get; set; }
    }

    [DataContract]
    public class SubmittedWord
    {
        [DataMember]
        public string Word { get; set; }
    }

    [DataContract]
    public class ValidationResponse
    {
        [DataMember]
        public bool IsValid { get; set; }
    }
}