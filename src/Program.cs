using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using System.Configuration;
using Newtonsoft.Json;
using CommandLine;
using System.Data.Entity;
using Shindy.Core.Entities;
using Shindy.Data.SqlServer;
using Shindy.Dmn.Loader.Dnm;
using Session = Shindy.Dmn.Loader.Dnm.Session;

namespace Shindy.Dmn.Loader
{
    class Program
    {

        static void Main(string[] args)
        {
            var options = new Options();
            Parser parser = new Parser();

            parser.ParseArguments(args, options);

            if (options.JsonPath == null) { options.JsonPath = ConfigurationManager.AppSettings["JSONPath"]; }
            
            //TODO: Not used anymore
            //if (options.ServerName == null) { options.ServerName = ConfigurationManager.AppSettings["ServerName"]; }
            //if (options.DBName == null) { options.DBName = ConfigurationManager.AppSettings["DBName"]; }
            //if (options.UserName == null) { options.UserName = ConfigurationManager.AppSettings["UserName"]; }

            if (options.DeleteExistingData) { DeleteExistingData(options.ResetIds); }
            LoadEvents(options);

            Console.WriteLine("JSON file successfully loaded into {0}.", options.DBName);
            Console.ReadKey();
        }

        public static void DeleteExistingData(bool reseedTables)
        {
            using (var shindyContext = new Shindy.Data.SqlServer.ShindyContext())
            {

                var deleteSql = @"DELETE FROM Attendee;
                                  DELETE FROM Giveaway;
                                  DELETE FROM EventSponsor;
                                  DELETE FROM Sponsor;
                                  DELETE FROM Speaker;
                                  DELETE FROM Person;
                                  DELETE FROM EventSession;
                                  DELETE FROM Session;
                                  DELETE FROM OrgEvent;
                                  DELETE FROM Event;
                                  DELETE FROM Location;
                                  DELETE FROM SessionType;
                                  DELETE FROM Organization;";
                shindyContext.Database.ExecuteSqlCommand(deleteSql);

                if (reseedTables)
                {
                    var reseedSql = @"DBCC CHECKIDENT ('Attendee', RESEED, 0);
                                      DBCC CHECKIDENT ('Giveaway', RESEED, 0);
                                      DBCC CHECKIDENT ('EventSponsor', RESEED, 0);
                                      DBCC CHECKIDENT ('Sponsor', RESEED, 0);
                                      DBCC CHECKIDENT ('Speaker', RESEED, 0);
                                      DBCC CHECKIDENT ('Person', RESEED, 0);
                                      DBCC CHECKIDENT ('EventSession', RESEED, 0);
                                      DBCC CHECKIDENT ('Session', RESEED, 0);
                                      DBCC CHECKIDENT ('OrgEvent', RESEED, 0);
                                      DBCC CHECKIDENT ('Event', RESEED, 0);
                                      DBCC CHECKIDENT ('Location', RESEED, 0);
                                      DBCC CHECKIDENT ('Organization', RESEED, 0);";
                    shindyContext.Database.ExecuteSqlCommand(reseedSql);
                }
            }
        }

        public static void LoadEvents(Options options)
        {
            var events = GetJsonData<Dnm.DnmEvents>(options.JsonPath);

            using (var shindyContext = new Shindy.Data.SqlServer.ShindyContext())
            {

                foreach (Dnm.Event evnt in events.Events.OrderBy(e=>e.EventDateTime))
                {
                    evnt.EventDateTime = evnt.EventDateTime.ToUniversalTime();

                    var dnmEvent = (from ev in shindyContext.Events
                                    .Include(p => p.EventSessions)
                                    .Include(p => p.EventSponsors)
                                    .Include(p => p.Location)
                                    where ev.Title == evnt.Title
                                    && ev.StartDate == evnt.EventDateTime
                                    select ev).FirstOrDefault();

                    if (dnmEvent == null)
                    {
                        dnmEvent = CreateDnmEvent(evnt);
                        shindyContext.Events.Add(dnmEvent);
                    }

                    if (evnt.HostedGroups != null)
                    {
                        foreach (var hg in evnt.HostedGroups)
                        {
                            var dnmOrg = shindyContext.Organizations.Where(p => p.Name == hg.Name).FirstOrDefault();
                            if (dnmOrg == null && hg.Name != "")
                            {

                                dnmOrg = CreateDnmOrg(hg);
                                if (dnmOrg != null)
                                {
                                    var dnmOrgEvent = CreateDnmOrgEvent(dnmOrg, dnmEvent);
                                    dnmEvent.OrgEvents.Add(dnmOrgEvent);
                                }
                            }
                        }
                    }

                    if (evnt.Sponsors != null)
                    {
                        foreach (var spon in evnt.Sponsors)
                        {
                            var dnmSponsor =
                                shindyContext.Sponsors.Where(p => p.Name == spon.Name).FirstOrDefault();

                            if (dnmSponsor == null && spon.Name != "")
                            {
                                dnmSponsor = CreateDnmSponsor(spon);
                            }

                            if (dnmSponsor != null)
                            {
                                var dnmEventSponsor = CreateDnmEventSponsor(dnmSponsor);
                                dnmEvent.EventSponsors.Add(dnmEventSponsor);

                            }
                        }
                    }

                    if (evnt.EventLocation != null)
                    {
                        var dnmLocation =
                            shindyContext.Locations.Where(p => p.Name == evnt.EventLocation.Name).FirstOrDefault();

                        if (dnmLocation == null)
                        {
                            dnmLocation = CreateDnmLocation(evnt);
                        }
                        dnmEvent.Location = dnmLocation;
                    }

                    if (evnt.Sessions != null)
                    {
                        foreach (Dnm.Session sess in evnt.Sessions)
                        {
                            var dnmEventSession = dnmEvent.EventSessions.Where(p => p.Session.Title == sess.Title).FirstOrDefault();

                            if (dnmEventSession == null)
                            {
                                var dnmSessionType = shindyContext.SessionTypes.Where(p => p.Name == sess.SessionType).FirstOrDefault();
                                if (dnmSessionType == null)
                                {
                                    dnmSessionType = CreateDnmSessionType(sess);
                                }

                                var dnmSession = CreateDnmSession(sess, dnmSessionType);

                                if (sess.Speakers != null)
                                {
                                    foreach (var spkr in sess.Speakers)
                                    {
                                        var dnmPerson =
                                            shindyContext.People.Where(p => p.FirstName == spkr.FirstName
                                                && p.LastName == spkr.LastName).FirstOrDefault();

                                        if (dnmPerson == null)
                                        {
                                            dnmPerson = CreateDnmPerson(spkr);
                                        }
                                        if (dnmPerson.EMail == "" && spkr.Email != "") { dnmPerson.EMail = spkr.Email; }

                                        var dnmSpeaker = CreateDnmSpeaker(dnmPerson);
                                        dnmSession.Speakers.Add(dnmSpeaker);
                                    }
                                }

                                dnmEventSession = CreateDnmEventSession(dnmSession);

                                dnmEvent.EventSessions.Add(dnmEventSession);
                            }
                        }
                    }

                    try
                    {
                        shindyContext.SaveChanges();
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException e)
                    {
                        foreach (var eve in e.EntityValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                                eve.Entry.Entity.GetType().Name, eve.Entry.State);
                            foreach (var ve in eve.ValidationErrors)
                            {
                                System.Diagnostics.Debug.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                                    ve.PropertyName, ve.ErrorMessage);
                            }
                        }
                        throw;
                    }
                    Console.WriteLine("Saved Event {0}", evnt.Title + " " + evnt.EventDateTime.ToString());
                }
            }
        }

        private static Shindy.Core.Entities.Event CreateDnmEvent(Dnm.Event evnt)
        {
            var dnmEvent = new Shindy.Core.Entities.Event();
            dnmEvent.Title = evnt.Title;
            dnmEvent.StartDate = evnt.EventDateTime;
            dnmEvent.CreatedDate = DateTime.Now;
            dnmEvent.UpdatedDate = DateTime.Now;
            if (evnt.RegistrationURI != "")
            {
                dnmEvent.RegistrationURI = evnt.RegistrationURI;
            }
            if (evnt.Description != "")
            {
                dnmEvent.Description = evnt.Description;
            }
            return dnmEvent;
        }

        private static Shindy.Core.Entities.OrgEvent CreateDnmOrgEvent(Shindy.Core.Entities.Organization dnmOrg, Shindy.Core.Entities.Event dnmEvent)
        {
            var dnmOrgEvent = new Shindy.Core.Entities.OrgEvent();
            dnmOrgEvent.Event = dnmEvent;
            dnmOrgEvent.Organization = dnmOrg;
            dnmOrgEvent.CreatedDate = DateTime.Now;
            dnmOrgEvent.UpdatedDate = DateTime.Now;
            return dnmOrgEvent;
        }

        private static Shindy.Core.Entities.Organization CreateDnmOrg(Hostedgroup hg)
        {
            var dnmOrg = new Shindy.Core.Entities.Organization();
            dnmOrg.Name = hg.Name;
            if (hg.Name == "dotNet Miami")
            {
                dnmOrg.Description =
                    @"We are a group of developers that are passionate about technology. We primarily focus on Microsoft technologies but are open minded to other platforms and ways of thought. We are diverse in our experiences with technology some of us are seasoned corporate developers while others are students. Within the group we see each other as equals and are able to learn from our unique experiences.";
                dnmOrg.OrgUri = "http://dotnetmiami.com";
            }
            dnmOrg.CreatedDate = DateTime.Now;
            dnmOrg.UpdatedDate = DateTime.Now;

            return dnmOrg;
        }

        private static Shindy.Core.Entities.Location CreateDnmLocation(Dnm.Event evnt)
        {
            var dnmLocation = new Shindy.Core.Entities.Location();
            dnmLocation.Name = evnt.EventLocation.Name;
            if (evnt.EventLocation.LocationURI != "")
            {
                dnmLocation.LocationURI = evnt.EventLocation.LocationURI;
            }
            dnmLocation.Street1 = evnt.EventLocation.Address.Street;
            dnmLocation.City = evnt.EventLocation.Address.City;
            dnmLocation.State = evnt.EventLocation.Address.State;
            dnmLocation.ZipCode = evnt.EventLocation.Address.ZipCode;
            dnmLocation.MapURI = evnt.EventLocation.Address.AddressURL;
            dnmLocation.CreatedDate = DateTime.Now;
            dnmLocation.UpdatedDate = DateTime.Now;
            return dnmLocation;
        }

        private static Shindy.Core.Entities.Sponsor CreateDnmSponsor(Dnm.Sponsor spon)
        {
            var dnmSponsor = new Shindy.Core.Entities.Sponsor();
            dnmSponsor.Name = spon.Name;
            if (spon.SponsorURI != "")
            {
                dnmSponsor.SponsorURI = spon.SponsorURI;
            }
            if (spon.ImageURI != "")
            {
                dnmSponsor.ImageURI = spon.ImageURI;
            }
            dnmSponsor.CreatedDate = DateTime.Now;
            dnmSponsor.UpdatedDate = DateTime.Now;
            return dnmSponsor;
        }

        private static Shindy.Core.Entities.EventSponsor CreateDnmEventSponsor(Shindy.Core.Entities.Sponsor dnmSponsor)
        {
            var dnmEventSponsor = new Shindy.Core.Entities.EventSponsor();
            dnmEventSponsor.Sponsor = dnmSponsor;
            dnmEventSponsor.CreatedDate = DateTime.Now;
            dnmEventSponsor.UpdatedDate = DateTime.Now;
            return dnmEventSponsor;
        }

        private static Shindy.Core.Entities.Speaker CreateDnmSpeaker(Shindy.Core.Entities.Person dnmPerson)
        {
            var dnmSpeaker = new Shindy.Core.Entities.Speaker();
            dnmSpeaker.CreatedDate = DateTime.Now;
            dnmSpeaker.UpdatedDate = DateTime.Now;
            dnmSpeaker.Person = dnmPerson;
            return dnmSpeaker;
        }

        private static Shindy.Core.Entities.Person CreateDnmPerson(Dnm.Speaker spkr)
        {
            var dnmPerson = new Shindy.Core.Entities.Person();
            dnmPerson.FirstName = spkr.FirstName;
            dnmPerson.LastName = spkr.LastName;
            dnmPerson.CreatedDate = DateTime.Now;
            dnmPerson.UpdatedDate = DateTime.Now;
            if (spkr.Bio != "")
            {
                dnmPerson.Bio = spkr.Bio;
            }
            if (spkr.Email != "")
            {
                dnmPerson.EMail = spkr.Email;
            }
            if (spkr.PersonURI != "")
            {
                dnmPerson.MemberUri = spkr.PersonURI;
            }
            return dnmPerson;
        }

        private static Shindy.Core.Entities.Session CreateDnmSession(Dnm.Session sess, Shindy.Core.Entities.SessionType dnmSessionType)
        {
            var dnmSession = new Shindy.Core.Entities.Session();

            dnmSession.Title = sess.Title;
            if (dnmSession.Abstract != "")
            {
                dnmSession.Abstract = sess.Abstract;
            }
            dnmSession.DemoUri = sess.DemoURI;
            dnmSession.PresentationUri = sess.PresentationURI;
            dnmSession.CreatedDate = DateTime.Now;
            dnmSession.UpdatedDate = DateTime.Now;
            //if (dnmSession.PresentationURI != "") { dnmSession.PresentationURI = s.Abstract; }
            //if (dnmSession.DemoURI != "") { dnmSession.DemoURI = s.Abstract; }

            dnmSession.SessionType = dnmSessionType;
            return dnmSession;
        }

        private static Shindy.Core.Entities.EventSession CreateDnmEventSession(Shindy.Core.Entities.Session dnmSession)
        {
            EventSession dnmEventSession;
            dnmEventSession = new Shindy.Core.Entities.EventSession();
            dnmEventSession.CreatedDate = DateTime.Now;
            dnmEventSession.UpdatedDate = DateTime.Now;
            dnmEventSession.Session = dnmSession;
            return dnmEventSession;
        }

        private static Shindy.Core.Entities.SessionType CreateDnmSessionType(Session sess)
        {
            var dnmSessionType = new Shindy.Core.Entities.SessionType();
            dnmSessionType.Name = sess.SessionType;
            dnmSessionType.CreatedDate = DateTime.Now;
            dnmSessionType.UpdatedDate = DateTime.Now;
            return dnmSessionType;
        }

        public enum TransportType { http, file }

        public static T GetJsonData<T>(string path) where T : new()
        {
            var jsonData = string.Empty;

            if (DetermineTransport(path) == TransportType.http)
            {
                jsonData = GetURLJsonData(path);
            }
            else
            {
                jsonData = GetFileJsonData(path);
            }
            return LoadObjectFromJson<T>(jsonData);
        }

        public static T LoadObjectFromJson<T>(string jsonData) where T : new()
        {
            // if string with JSON data is not empty, deserialize it to class and return its instance 
            return !string.IsNullOrEmpty(jsonData) ? JsonConvert.DeserializeObject<T>(jsonData) : new T();
        }

        public static TransportType DetermineTransport(string path)
        {
            TransportType transportType = TransportType.file;

            if (path.Substring(0, 4) == "http")
            {
                transportType = TransportType.http;
            }
            return transportType;
        }

        public static string GetURLJsonData(string url)
        {
            string urlData = string.Empty;

            using (var web = new WebClient())
            {
                // attempt to download JSON data as a string
                try
                {
                    web.Encoding = Encoding.UTF8;
                    urlData = web.DownloadString(url);

                }
                catch (Exception)
                {
                    throw;
                }

            }
            return urlData;
        }

        public static string GetFileJsonData(string path)
        {
            string pathData = string.Empty;

            pathData = System.IO.File.ReadAllText(path);

            return pathData;
        }
    }

}

