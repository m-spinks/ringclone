using System.Web;
using System.Web.Mvc;

namespace RingClone.Portal
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleRingCloneErrorAttribute());
			filters.Add(new HandleErrorAttribute());
		}
	}
}