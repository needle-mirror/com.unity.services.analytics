# Analytics

The Analytics package lets you record events from your application. There are
two main types of events.

Type     | Description
---------|------------------------------------------------------------------------------
Standard | Standard events are predefined, some are automatic, others are manual.
Custom   | Events that are authored by the developer that contain custom data points.

## SDK Lifetime

Before the runtime will start sending events you need to call the initialize method. The
`userID` and `sessionID` are optional values; if they are not provided the SDK will
generate random IDs.

```cs
Events.Startup(userID, sessionID);
```

At the end of the session its recomended to call Shutdown to allow the internals
to shutdown, including recording the `gameEnded` event and attempting to upload
any waiting events.

```cs
Events.Shutdown();
```

## Logging Standard Events

Many standard events are logged automatically by the SDK. These automatic events will give you a basic picture
of how users are using it without any further intervention.

- The `sdkStart` event will be recorded on Startup
- The `clientDevice` event will be recorded on Startup
- The `gameStarted` event will be recorded on Startup
- The `newPlayer` event will be recorded on Startup if the particular device has never started the app before
- The `gameRunning` event will be recorded every 1 minute
- The `gameEnded` event will be recorded on Shutdown

There are two standard events that you can record manually, at any point in the game lifecycle.

### Transaction Event

The Transaction event is to record that the player spent some resource to obtain some other resource.
This may be real or virtual currencies or game items on either side. Because of the number of optional
arguments depending on what is being exchanged and on what platform, the LogTransaction method takes
a `TransactionParameters` object which can be populated as required.

```cs
Events.Transaction(new Events.TransactionParameters
	{
		productsReceived = new Events.Product(),
		productsSpent = new Events.Product(),
		transactionName = "transactionName",
		transactionType = Events.TransactionType.SALE
	});
```

### AdImpression Event

The AdImpression event is to record that the player was shown an advert and how they interacted with it.

```cs
Events.AdImpression(Events.AdCompletionStatus.Completed,
					Events.AdProvider.UnityAds,
					"PLACEMENT_ID",
					"PLACEMENT_NAME",
					null, null, null, null, null, null, null, null, null, null, null, null));
```

## Logging Custom Events

At any point you may send custom events. Custom events must have a schema defined on the dashboard
or else they will be rejected by the collector (they will still be sent by the SDK).

```cs
Events.CustomData("customEventName", new Dictionary<string, object> { { "parameter": "value" } });
```

## Sending Events

Event data will be flushed out of the system every 60 seconds, and on Shutdown.

You can also force a flush at a time of your choosing:

```cs
Events.Flush();
```

You shouldn't need to do this in normal usage.
