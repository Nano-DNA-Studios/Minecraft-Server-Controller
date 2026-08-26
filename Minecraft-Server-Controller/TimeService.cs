
namespace Minecraft_Server_Controller
{
    public abstract class TimeService : BackgroundService
    {
        protected ServerManager _Manager { get; set; }

        protected ServerSettings Settings { get; set; }

        public TimeService(ServerManager manager, ServerSettings settings)
        {
            _Manager = manager;
            Settings = settings;
        }

        protected TimeSpan GetDelay(bool hourly)
        {
            DateTime now = DateTime.Now;
            DateTime target;

            if (hourly)
            {
                target = new DateTime(
                    now.Year,
                    now.Month,
                    now.Day,
                    now.Hour,
                    0,
                    0,
                    now.Kind
                ).AddHours(1);
            }
            else
            {
                target = new DateTime(
                     now.Year,
                     now.Month,
                     now.Day,
                     23,
                     55,
                     0,
                     now.Kind
                 );

                if (target <= now)
                    target = target.AddDays(1);
            }

            return target - now;
        }

        //protected TimeSpan GetDelay(bool hourly)
        //{
        //    DateTime now = DateTime.Now;
        //    DateTime target;

        //    if (hourly)
        //    {
        //        target = new DateTime(
        //            now.Year,
        //            now.Month,
        //            now.Day,
        //            now.Hour,
        //            now.Minute,
        //            0,
        //            now.Kind
        //        ).AddMinutes(1);
        //    }
        //    else
        //    {
        //        target = new DateTime(
        //             now.Year,
        //             now.Month,
        //             now.Day,
        //             now.Hour,
        //             now.Minute,
        //             0,
        //             now.Kind
        //         ).AddMinutes(5);

        //        //if (target <= now)
        //        //    target = target.AddDays(1);
        //    }

        //    return target - now;
        //}
    }
}
