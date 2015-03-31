using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shindy.Dmn.Loader.Dnm
{

	public class DnmEvents
	{
		public Event[] Events { get; set; }
	}

	public class Event
	{
		public int EventID { get; set; }
		public DateTime EventDateTime { get; set; }
		public string Title { get; set; }
		public Hostedgroup[] HostedGroups { get; set; }
		public string Description { get; set; }
		public string RegistrationURI { get; set; }
		public Eventlocation EventLocation { get; set; }
		public Session[] Sessions { get; set; }
		public Sponsor[] Sponsors { get; set; }
		public string EventTime { get; set; }
		public string EventDate { get; set; }
	}

	public class Eventlocation
	{
		public int LocationID { get; set; }
		public string Name { get; set; }
		public string LocationURI { get; set; }
		public Address Address { get; set; }
	}

	public class Address
	{
		public string Street { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string ZipCode { get; set; }
		public string AddressURL { get; set; }
	}

	public class Hostedgroup
	{
		public string Name { get; set; }
		public string GroupURI { get; set; }
	}

	public class Session
	{
		public string Title { get; set; }
		public string SessionType { get; set; }
		public string Abstract { get; set; }
		public string PresentationURI { get; set; }
		public string DemoURI { get; set; }
		public Speaker[] Speakers { get; set; }
		public string _abstract { get; set; }
	}

	public class Speaker
	{
		public int PersonID { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public string PersonURI { get; set; }
		public string Bio { get; set; }
	}

	public class Sponsor
	{
		public object SponsorID { get; set; }
		public string Name { get; set; }
		public string ImageURI { get; set; }
		public string SponsorURI { get; set; }
	}
}
