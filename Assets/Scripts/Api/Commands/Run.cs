/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;

namespace Api.Commands
{
    class Run : ICommand
    {
        public string Name { get { return "simulator/run"; } }

        public void Execute(JSONNode args)
        {
            var time_limit = args["time_limit"].AsFloat;
            if (time_limit != 0)
            {
                ApiManager.Instance.TimeLimit = ApiManager.Instance.CurrentTime + time_limit;
            }
            else
            {
                ApiManager.Instance.TimeLimit = 0.0;
            }
            Time.timeScale = 1.0f;
        }
    }
}
