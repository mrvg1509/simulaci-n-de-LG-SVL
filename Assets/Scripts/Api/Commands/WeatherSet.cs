/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;

namespace Api.Commands
{
    class WeatherSet : ICommand
    {
        public string Name { get { return "environment/weather/set"; } }

        public void Execute(JSONNode args)
        {
            var env = EnvironmentEffectsManager.Instance;
            if (env == null)
            {
                ApiManager.Instance.SendError("Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            env.rainIntensitySlider.value = args["rain"].AsFloat;
            env.fogIntensitySlider.value = args["fog"].AsFloat;
            env.roadWetnessSlider.value = args["wetness"].AsFloat;

            ApiManager.Instance.SendResult();
        }
    }
}
