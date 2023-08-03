using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Mozilla;
using Xrm.Tools.WebAPI;
using Xrm.Tools.WebAPI.Requests;
using static System.Net.Mime.MediaTypeNames;

namespace EMBC.DFA.API.ConfigurationModule.Models.Dynamics
{
    public class DynamicsGateway : IDynamicsGateway
    {
        private readonly CRMWebAPI api;

        public DynamicsGateway(CRMWebAPI api)
        {
            this.api = api;
        }

        public async Task<IEnumerable<dfa_appcontact>> GetContactsAsync()
        {
            var list = await api.GetList<dfa_appcontact>("dfa_appcontacts", new CRMGetListOptions
            {
                Select = new[] { "dfa_firstname", "dfa_lastname", "dfa_initial" }
            });

            return list.List;
        }

        public async Task<IEnumerable<Country>> GetCountriesAsync()
        {
            var list = await api.GetList<Country>("era_countries", new CRMGetListOptions
            {
                Select = new[] { "era_countrycode", "era_countryid", "era_isocountrycode", "era_name" }
            });

            return list.List;
        }

        public async Task<dfa_appcontact> GetUserProfileAsync(string userId)
        {
            try
            {
                var userObj = await api.GetList<dfa_appcontact>("dfa_appcontacts", new CRMGetListOptions
                {
                    Select = new[]
                    {
                        "dfa_firstname", "dfa_lastname", "dfa_initial",
                        "dfa_isindigenous", "dfa_emailaddress", "dfa_cellphonenumber",
                        "dfa_residencetelephonenumber", "dfa_alternatephonenumber", "dfa_primaryaddressline1",
                        "dfa_primaryaddressline2", "dfa_primarycity", "dfa_primarypostalcode",
                        "dfa_primarystateprovince", "dfa_secondaryaddressline1", "dfa_secondaryaddressline2",
                        "dfa_secondarycity", "dfa_secondarypostalcode", "dfa_secondarystateprovince",
                        "dfa_isprimaryandsecondaryaddresssame", "dfa_appcontactid",
                        "dfa_bcservicecardid"
                    },
                    Filter = $"dfa_bcservicecardid eq '{userId}'"
                });

                return userObj != null ? userObj.List.LastOrDefault() : null;
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to obtain access token from {ex.Message}", ex);
            }
        }

        public async Task<string> AddContact(dfa_appcontact contact)
        {
            try
            {
                var result = await api.ExecuteAction("dfa_DFAPortalCreateProfile", contact);

                if (result != null)
                {
                    return result.Where(m => m.Key == "output") != null ? result.Where(m => m.Key == "output").ToList()[0].Value.ToString() : string.Empty;
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to add contact {ex.Message}", ex);
            }

            return string.Empty;
        }

        // TODO: fails with null dates when signatures not passed in
        public async Task<string> AddApplication(dfa_appapplicationstart_params application)
        {
            try
            {
                var result = await api.ExecuteAction("dfa_DFAPortalCreateApplication", application);

                if (result != null)
                {
                    return result.Where(m => m.Key == "output") != null ? result.Where(m => m.Key == "output").ToList()[0].Value.ToString() : string.Empty;
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to add application {ex.Message}", ex);
            }

            return string.Empty;
        }

        // TODO: Fails with resource not found for the segment
        public async Task<System.Dynamic.ExpandoObject> AddApplicationSignatures(IEnumerable<dfa_signature> dfa_signatures)
        {
            try
            {
                return await api.ExecuteAction("dfa_DFAPortalCreateApplicationAnnotation", dfa_signatures);
            }
            // catch (System.Exception ex)
            catch
            {
                return new System.Dynamic.ExpandoObject();
                // throw new Exception($"Failed to add application signatures {ex.Message}", ex);
            }
        }

        // TODO: missing parameters dfa_acopyofarentalagreementorlease (existing field), dfa_areyounowresidingintheresidence (existing),
        // dfa_causeofdamageflood (new field), dfa_causeofdamagewildfire (new field), dfa_causeofdamagelandslide (new field), dfa_causeofdamagestorm (new field), dfa_causeofdamageother (new field)
        // dfa_causeofdamageloss (existing field), dfa_dateofdamage (existing field), dfa_dateofdamageto (existing field), dfa_datereturntoresidence (existing field)
        // dfa_description (existing field), dfa_doyourlossestotalmorethan1000 (existing field), dfa_havereceiptsforcleanupsorrepairs (existing field)
        // dfa_wereyouevacuatedduringtheevent (existing field), dfa_primaryapplicantprintname, dfa_primaryapplicantsigned, dfa_primaryapplicantsigneddate,
        // dfa_secondaryapplicantsigned, dfa_secondaryapplicantprintname, dfa_secondaryapplicantsigneddate
        public async Task<string> UpdateApplication(dfa_appapplicationmain_params application)
        {
            try
            {
                var result = await api.ExecuteAction("dfa_DFAPortalCreateApplication", application);

                if (result != null)
                {
                    return result.Where(m => m.Key == "output") != null ? result.Where(m => m.Key == "output").ToList()[0].Value.ToString() : string.Empty;
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to update/delete application {ex.Message}", ex);
            }

            return string.Empty;
        }

        // TODO: missing fields for no ins signatures
        public async Task<dfa_appapplicationstart_retrieve> GetApplicationStartById(Guid applicationId)
        {
            var list = await api.GetList<dfa_appapplicationstart_retrieve>("dfa_appapplications", new CRMGetListOptions
            {
                Select = new[]
                {
                    "dfa_appapplicationid", "dfa_applicanttype", "dfa_doyouhaveinsurancecoverage2", "_dfa_applicant_value"
                    // "dfa_primaryapplicantsignednoins"
                    // "dfa_primaryapplicantprintnamenoins", "dfa_primaryapplicantsigneddatenoins", "dfa_secondaryapplicantsignednoins",
                    // "dfa_secondaryapplicantprintnamenoins", "dfa_secondaryapplicantsigneddatenoins"
                },
                Filter = $"dfa_appapplicationid eq {applicationId}"
            });

            return list.List.FirstOrDefault();
        }

        // TODO: missing fields "dfa_causeofdamageflood", "dfa_causeofdamagestorm", "dfa_causeofdamagewildfire", "dfa_causeofdamagelandslide", "dfa_causeofdamageother",
        public async Task<dfa_appapplicationmain_retrieve> GetApplicationMainById(Guid applicationId)
        {
            var list = await api.GetList<dfa_appapplicationmain_retrieve>("dfa_appapplications", new CRMGetListOptions
            {
                // TODO Update list of fields
                Select = new[]
                {
                    "dfa_appapplicationid", "dfa_isprimaryanddamagedaddresssame", "dfa_damagedpropertystreet1", "dfa_damagedpropertystreet2",
                    "dfa_damagedpropertycitytext", "dfa_damagedpropertyprovince", "dfa_damagedpropertypostalcode", "dfa_isthispropertyyourp",
                    "dfa_indigenousreserve", "dfa_nameoffirstnationsr", "dfa_manufacturedhom", "dfa_eligibleforbchomegrantonthisproperty",
                    "_dfa_confirmedbuildinglandlord_value",
                    "dfa_acopyofarentalagreementorlease", "dfa_areyounowresidingintheresidence",
                    // "dfa_causeofdamageflood", "dfa_causeofdamagestorm", "dfa_causeofdamagewildfire", "dfa_causeofdamagelandslide", "dfa_causeofdamageother",
                    "dfa_causeofdamageloss", "dfa_dateofdamage", "dfa_dateofdamageto", "dfa_datereturntotheresidence",
                    "dfa_description", "dfa_doyourlossestotalmorethan1000", "dfa_haveinvoicesreceiptsforcleanuporrepairs",
                    "dfa_primaryapplicantprintname", "dfa_primaryapplicantsigned", "dfa_primaryapplicantsigneddate",
                    "dfa_secondaryapplicantprintname", "dfa_secondaryapplicantsigned", "dfa_secondaryapplicantsigneddate"
                },
                Filter = $"dfa_appapplicationid eq {applicationId}"
            });

            foreach (dfa_appapplicationmain_retrieve application in list.List)
            {
                if (application._dfa_confirmedbuildinglandlord_value != null)
                {
                    var buildingOwnerlist = await api.GetList<dfa_appbuildingownerlandlord>("dfa_appbuildingownerlandlord", new CRMGetListOptions
                    {
                        Select = new[]
                        {
                            "dfa_contactlastname", "dfa_contactphone1", "dfa_contactemail", "dfa_contactfirstname"
                        },
                        Filter = $"dfa_appbuildingownerlandlordid eq {application._dfa_confirmedbuildinglandlord_value}"
                    });

                    application.dfa_contactfirstname = buildingOwnerlist.List.Last().dfa_contactfirstname;
                    application.dfa_contactlastname = buildingOwnerlist.List.Last().dfa_contactlastname;
                    application.dfa_contactphone1 = buildingOwnerlist.List.Last().dfa_contactphone1;
                    application.dfa_contactemail = buildingOwnerlist.List.Last().dfa_contactemail;
                }
            }

            return list.List.FirstOrDefault();
        }

        public async Task<IEnumerable<dfa_appapplication>> GetApplicationListAsync(string profileId)
        {
            try
            {
                var lstEvents = await api.GetList<dfa_event>("dfa_events", new CRMGetListOptions
                {
                    Select = new[]
                    {
                        "dfa_eventid", "dfa_id"
                    }
                });

                var lstCases = await api.GetList<dfa_incident>("incidents", new CRMGetListOptions
                {
                    Select = new[]
                    {
                        "incidentid", "ticketnumber"
                    }
                });

                var list = await api.GetList<dfa_appapplication>("dfa_appapplications", new CRMGetListOptions
                {
                    Select = new[]
                    {
                        "dfa_appapplicationid", "dfa_applicanttype",
                        "dfa_dateofdamage", "dfa_damagedpropertystreet1", "dfa_damagedpropertycitytext", "_dfa_eventid_value", "_dfa_casecreatedid_value"
                    },
                    Filter = $"_dfa_applicant_value eq {profileId}"
                    //Expand = new CRMExpandOptions[]
                    //{
                    //    new CRMExpandOptions()
                    //    {
                    //        Property = "_dfa_eventid_value",
                    //        Select = new string[] { "dfa_eventid", "dfa_id" }
                    //    }
                    //}
                });

                var lstApps = (from objApp in list.List
                            from objEvent in lstEvents.List
                            where objEvent.dfa_eventid == objApp._dfa_eventid_value
                            from objCase in lstCases.List
                            where objCase.incidentid == objApp._dfa_casecreatedid_value
                            select new dfa_appapplication
                            {
                                dfa_appapplicationid = objApp.dfa_appapplicationid,
                                dfa_applicanttype = objApp.dfa_applicanttype,
                                dfa_dateofdamage = objApp.dfa_dateofdamage,
                                dfa_damagedpropertystreet1 = objApp.dfa_damagedpropertystreet1,
                                dfa_damagedpropertycitytext = objApp.dfa_damagedpropertycitytext,
                                dfa_event = objEvent.dfa_id,
                                dfa_casenumber = objCase.ticketnumber
                            }).AsEnumerable();

                return lstApps;
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to obtain access token from {ex.Message}", ex);
            }
        }

        public async Task<string> UpsertDeleteDamagedItemAsync(dfa_appdamageditems_params objDamagedItems)
        {
            try
            {
                var result = await api.ExecuteAction("dfa_DFAPortalCreateAppDamagedItem", objDamagedItems);

                if (result != null)
                {
                    return result.Where(m => m.Key == "output") != null ? result.Where(m => m.Key == "output").ToList()[0].Value.ToString() : string.Empty;
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to update/delete/insert damaged items {ex.Message}", ex);
            }

            return string.Empty;
        }

        public async Task<IEnumerable<dfa_appdamageditems_retrieve>> GetDamagedItemsListAsync(Guid applicationId)
        {
            try
            {
                var list = await api.GetList<dfa_appdamageditems_retrieve>("dfa_appdamageditems", new CRMGetListOptions
                {
                    Select = new[] { "_dfa_applicationid_value", "dfa_appdamageditemid", "dfa_roomname", "dfa_damagedescription" },
                    Filter = $"_dfa_applicationid_value eq {applicationId}"
                });

                return list.List;
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to get damaged items {ex.Message}", ex);
            }
        }

        public async Task<string> UpsertDeleteSecondaryApplicantAsync(dfa_appsecondaryapplicant_params objSecondaryApplicant)
        {
            try
            {
                var result = await api.ExecuteAction("dfa_DFAPortalAppSecondaryApplicant", objSecondaryApplicant);

                if (result != null)
                {
                    return result.Where(m => m.Key == "output") != null ? result.Where(m => m.Key == "output").ToList()[0].Value.ToString() : string.Empty;
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to update/insert/delete secondary applicant {ex.Message}", ex);
            }

            return string.Empty;
        }

        public async Task<IEnumerable<dfa_appsecondaryapplicant_retrieve>> GetSecondaryApplicantsListAsync(Guid applicationId)
        {
            try
            {
                var list = await api.GetList<dfa_appsecondaryapplicant_retrieve>("dfa_appsecondaryapplicants", new CRMGetListOptions // TODO by application Id
                {
                    Select = new[]
                    {
                        "_dfa_appapplicationid_value", "dfa_appsecondaryapplicantid", "dfa_applicanttype", "dfa_emailaddress",
                        "dfa_firstname", "dfa_lastname", "dfa_phonenumber"
                    },
                    Filter = $"_dfa_appapplicationid_value eq {applicationId}"
                });

                return list.List;
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to get secondary applicants {ex.Message}", ex);
            }
        }

        public async Task<string> UpsertDeleteOtherContactAsync(dfa_appothercontact_params objOtherContact)
        {
            try
            {
                var result = await api.ExecuteAction("dfa_DFAPortalOtherContact", objOtherContact);

                if (result != null)
                {
                    return result.Where(m => m.Key == "output") != null ? result.Where(m => m.Key == "output").ToList()[0].Value.ToString() : string.Empty;
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to update/insert/delete other contact {ex.Message}", ex);
            }

            return string.Empty;
        }

        public async Task<IEnumerable<dfa_appothercontact_retrieve>> GetOtherContactsListAsync(Guid applicationId)
        {
            try
            {
                var list = await api.GetList<dfa_appothercontact_retrieve>("dfa_appothercontacts", new CRMGetListOptions // TODO by applciation id
                {
                    Select = new[]
                    {
                        "_dfa_appapplicationid_value", "dfa_appothercontactid", "dfa_emailaddress", "dfa_firstname",
                        "dfa_lastname", "dfa_phonenumber", "dfa_name"
                    },
                    Filter = $"_dfa_appapplicationid_value eq {applicationId}"
                });

                return list.List;
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to get other contacts {ex.Message}", ex);
            }
        }

        public async Task<string> UpsertDeleteFullTimeOccupantAsync(dfa_appoccupant_params objAppOccupant)
        {
            try
            {
                var result = await api.ExecuteAction("dfa_DFAPortalAppOccupant", objAppOccupant);
                if (result != null)
                {
                    return result.Where(m => m.Key == "output") != null ? result.Where(m => m.Key == "output").ToList()[0].Value.ToString() : string.Empty;
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to update/insert/delete full time occupant {ex.Message}", ex);
            }

            return string.Empty;
        }

        public async Task<IEnumerable<dfa_appoccupant_retrieve>> GetFullTimeOccupantsListAsync(Guid applicationId)
        {
            try
            {
                var list = await api.GetList<dfa_appoccupant_retrieve>("dfa_appoccupants", new CRMGetListOptions
                {
                    Select = new[]
                    {
                        "_dfa_applicationid_value", "dfa_appoccupantid", "_dfa_contactid_value", "dfa_name"
                    },
                    Filter = $"_dfa_applicationid_value eq {applicationId}"
                });

                foreach (dfa_appoccupant_retrieve item in list.List)
                {
                    var contactList = await api.GetList<dfa_appcontact_extended>("dfa_appcontacts", new CRMGetListOptions
                    {
                        Select = new[]
                        {
                            "dfa_appcontactid", "dfa_firstname", "dfa_lastname", "dfa_title"
                        },
                        Filter = $"dfa_appcontactid eq {item._dfa_contactid_value}"
                    });

                    if (contactList != null)
                    {
                        item.dfa_title = contactList.List.Last().dfa_title;
                        item.dfa_firstname = contactList.List.Last().dfa_firstname;
                        item.dfa_lastname = contactList.List.Last().dfa_lastname;
                    }
                }

                return list.List;
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to get full time occupants {ex.Message}", ex);
            }
        }

        public async Task<string> UpsertDeleteCleanUpLogItemAsync(dfa_appcleanuplogs_params objCleanUpLog)
        {
            try
            {
                var result = await api.ExecuteAction("dfa_DFAPortalCreateAppCleanupLog", objCleanUpLog);

                if (result != null)
                {
                    return result.Where(m => m.Key == "output") != null ? result.Where(m => m.Key == "output").ToList()[0].Value.ToString() : string.Empty;
                }
            }
            catch (System.Exception ex)
            {
              throw new Exception($"Failed to update/insert/delete clean up log item {ex.Message}", ex);
            }

            return string.Empty;
        }

        public async Task<IEnumerable<dfa_appcleanuplogs_retrieve>> GetCleanUpLogItemsListAsync(Guid applicationId)
        {
            try
            {
                var list = await api.GetList<dfa_appcleanuplogs_retrieve>("dfa_appcleanuplogs", new CRMGetListOptions
                {
                    Select = new[]
                    {
                        "_dfa_applicationid_value", "dfa_appcleanuplogid", "dfa_name", "dfa_date", "dfa_hoursworked", "_dfa_contactid_value"
                    },
                    Filter = $"_dfa_applicationid_value eq {applicationId}"
                });

                foreach (var item in list.List)
                {
                    item.dfa_description = item.dfa_name; // name from cleanup logs entity is description of work
                    if (item._dfa_contactid_value != null)
                    {
                        var contactList = await api.GetList<dfa_appcontact_extended>("dfa_appcontacts", new CRMGetListOptions
                        {
                            Select = new[]
                            {
                            "dfa_name"
                            },
                            Filter = $"dfa_appcontactid eq {item._dfa_contactid_value}"
                        });

                        if (contactList != null)
                        {
                            item.dfa_name = contactList.List.Last().dfa_name;
                        }
                    }
                }

                return list.List;
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Failed to get clean up log items {ex.Message}", ex);
            }
        }

        public async Task<string> UpsertDeleteDocumentLocationAsync(dfa_appdocumentlocation_params objDocumentLocation)
        {
            try
            {
                /* if (objDocumentLocation?.dfa_appdocumentlocationid != null && objDocumentLocation?.delete == true)
                {
                    await api.Delete("dfa_appdocumentlocations", (System.Guid)objDocumentLocation.dfa_appdocumentlocationid);
                    return "Deleted successfully";
                }
                else if (objDocumentLocation?.dfa_appdocumentlocationid != null)
                {
                    var result = api.Update("dfa_appdocumentlocations", (System.Guid)objDocumentLocation.dfa_appdocumentlocationid, objDocumentLocation);
                    return "Updated successfully";
                }
                else if (objDocumentLocation?.dfa_appdocumentlocationid == null)
                {
                    var result = await api.Create("dfa_appdocumentlocations", objDocumentLocation);
                    return result.ToString();
                } */

                var result = await api.ExecuteAction("dfa_DFAPortalAppDocumentLocation", objDocumentLocation);

                if (result != null)
                {
                    return result.Where(m => m.Key == "output") != null ? result.Where(m => m.Key == "output").ToList()[0].Value.ToString() : string.Empty;
                }
            }
            // catch (System.Exception ex)
            catch
            {
                return Guid.Empty.ToString();
                // throw new Exception($"Failed to update document location {ex.Message}", ex);
            }

            return string.Empty;
        }

        // TODO : fails
        public async Task<IEnumerable<dfa_appdocumentlocation_retrieve>> GetDocumentLocationsListAsync(Guid applicationId)
        {
            try
            {
                var list = await api.GetList<dfa_appdocumentlocation_retrieve>("dfa_appdocumentlocations", new CRMGetListOptions
                {
                    Select = new[]
                    {
                        "_dfa_applicationid_value", "dfa_appdocumentlocationid", "dfa_name", "dfa_documenttype", "dfa_documentdescription",
                        "dfa_uploadeddate", "dfa_modifiedby", "dfa_filedata", "dfa_contenttype", "dfa_filesize"
                    },
                    Filter = $"_dfa_applicationid_value eq {applicationId}"
                });

                return list.List;
            }
            // catch (System.Exception ex)
            catch
            {
                return null;
                // throw new Exception($"Failed to get document locations {ex.Message}", ex);
            }
        }
    }
}
