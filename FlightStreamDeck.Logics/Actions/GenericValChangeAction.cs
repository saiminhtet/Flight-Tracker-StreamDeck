using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using System;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class GenericValChangeAction : StreamDeckAction
    {
        private readonly ILogger<ApToggleAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private string header = "";
        private TOGGLE_EVENT? toggleEvent = null;
        private bool isIncrease = true;

        public GenericValChangeAction(ILogger<ApToggleAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            setValues(args.Payload.Settings);
            RegisterValues();
            await UpdateImage();
        }

        private void setValues(JObject settings)
        {
            logger.LogInformation("Incoming: {1}", settings.ToString());

            string newHeader = settings.Value<string>("Header");
            string newAction = settings.Value<string>("TypeValue");
            TOGGLE_EVENT? newToggleEvent = GetEventValue(settings.Value<string>("AffectValue"));

            header = newHeader;
            toggleEvent = newToggleEvent;
            isIncrease = newAction == "+";
        }

        private TOGGLE_EVENT? GetEventValue(string value)
        {
            if (value == null)
            {
                return null;
            }

            TOGGLE_EVENT result;
            if (Enum.TryParse(value, true, out result))
            {
                return result;
            }

            return null;
        }

        protected override Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            setValues(args.Payload);
            _= UpdateImage();
            return Task.CompletedTask;
        }

        private void RegisterValues()
        {
            if (toggleEvent.HasValue) flightConnector.RegisterToggleEvent(toggleEvent.Value);
            flightConnector.RegisterToggleEvent(TOGGLE_EVENT.PLUS);
            flightConnector.RegisterToggleEvent(TOGGLE_EVENT.MINUS);
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            if (toggleEvent.HasValue)
            {
                flightConnector.Toggle(toggleEvent.Value);
                flightConnector.Toggle(isIncrease ? TOGGLE_EVENT.PLUS : TOGGLE_EVENT.MINUS);
            }
            return Task.CompletedTask;
        }

        private async Task UpdateImage()
        {
            await SetImageAsync(imageLogic.GetImage(header, true, isIncrease ? "+" : "-"));
        }
    }
}
