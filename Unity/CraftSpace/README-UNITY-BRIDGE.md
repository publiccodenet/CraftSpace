# **Json Unity Bridge**

This repo "bridge_unity" contains a JSON/C# Bridge for for controlling
and interrogating Unity3D via HTTP (and other transport protocols).

It's based on the open source UnityJS project I (Don Hopkins)
developed over a few years, which I have used for a few projects.

This version uses a simple open source Unity HTTP server
(UniWebServer) for communication. It can also talk through other
channels like SocketIO or directly to the JavaScript interpreter in
the web browser (for WebGL builds), or create its own JavaScript
interpreter (for iOS, Android, and desktop builds).

This is a generic messaging bridge, completely data driven, using C#
reflection, so you can query almost any property and send almost any
message to any object with any parameter and convert any kind of data
structures to and from json.

And it's easy to plug in custom json converters extending JSON.Net's
wonderful JSON converter system (which I've already done for many
popular unity data types, and have been using in other projects for
several years, so it's pretty solid code).

There are also a bunch of handy extension methods that make it more
convenient to do common stuff with JSON.net. It's a lot more powerful
and flexible than Unity's JSONUtility stuff.

It may be useful for replacing a lot of the MTPLink and the studio
server code, and it will be very useful to get new projects up and
running, since it enables us to control and debug the app remotely
over http, without writing a bunch of user interface crap.

There is some more documentation and architectural overview in the
original UnityJS repo:

https://github.com/SimHacker/bridge/blob/master/doc/Anatomy.txt

https://github.com/SimHacker/bridge/blob/master/doc/Architecture.txt

It will take some more explanation to make it obvious how this actually works, but here is a json file with events in it that play a random plug!

```javascript
[
    {
        "event": "Update",
        "id": "bridge",
        "data": {
            "targetTransform/component:MindtwinManager/method:PlayRandomPlug": []
        }
    }
]
```

And you can send it to unity like this:

+ `curl -X POST  -H "Content-Type: application/json" -d @Json/play-random-plug.json http://localhost:7777/send_events`

And here is a json file with a Query event that asks for the value of the studio_server_url property of the MindtwinManager component of the target transform (which is the "main object" that you point the bridge to, that it can serve).

```javascript
[
    {
        "event": "Query",
        "id": "bridge",
        "data": {
            "callbackID": "1",
            "query": {
                "studio_server_url": "targetTransform/component:MindtwinManager/studio_server_url"
            }
        }
    }
]
```

After you send that event, you have to ask to receive the response event back, with another query.

+ `curl -X POST  -H "Content-Type: application/json" -d @Json/query-studio-server-url.json http://localhost:7777/send_events`

```javascript
{
  "error": false,
  "message": "sent 1 events"
} (edited) 
```

+ `curl http://localhost:7777/receive_events`

```javascript
[{
  "event": "Callback",
  "id": "1",
  "data": {
    "studio_server_url": "http://192.168.2.153:9122"
  }]
```

There is a JavaScript library that implements the other end of the
bridge, but this is how to play with it manually by using curl from
the shell. It would be trivial to implement a compatible library in
Python, too.

A great thing to hook this up to would be the profiling library! And
we could use it to suck the log messages out of the app directly over
http, instead of using the studio server plumbing.

The transport is a plug-in class that supports different ways of
getting messages in and out. This is using the
BridgeTransportWebServer class. But we could also implement a
BridgeTransportRed5 to route messages through red5.

The Query event is super cool: it lets you query for a bunch of values
at once, giving a template that tells the name you want it sent back
to you as, and a "path expression" for each name, that can drill down
anywhere in the system and pick out an object (it knows how to
traverse C# object properties and even private fields, the Unity
transform tree, pick between the different Unity components on a
transform, and pick values out of C# arrays and lists and
dictionaries, and JSON arrays and lists, and even send messages with
parameters, access globally named variables, and other cool stuff
too), and then it uses the automatic C#<=>JSON conversion system to
convert it to json and return it! It's basically designed to give
convenient universal access to almost everything, and be easily
extensible to handle special cases and common tasks.


The whole protocol is designed to minimize round trips and message
size.

That's why you send a bunch of events at once, and ask to receive a
bunch of events at once.

You can also express interest in asynchronous events, and provide a
query template for when that event happens, that it uses to fill out
the response, so each interest can pick and choose what parameters it
wants sent back with the event. (Different event users are interested
in different things, so you don't want to send everything, and you
certainly don't want to perform another round trip query to ask what
the values you want are, so you can say up front what parameters to
send back with each event you are interested in.)

The Update event is the complement of the Query event. It has a "data"
key that contains a bunch of keys and values to update on the target
object (whose id is given in the event). But the keys are actually
path expressions, not necessarily properties directly on the target
object. So the keys of the update can drill down into other objects
that the target object can reach, and then, depending on the type of
the target, it calls the json type conversion machinery to convert the
json to the required destination type. So you can set a color
property, and using reflection, it knows to treat the json as a color
(either an html "#rrggbbaa" string or a {r:0,g:1,b:0,a:1} object).

You can also call methods by sending an Update event using a path
ending with "method:" to send a message to the object the path points
to, and pass parameters to the methods as an array value. So you could
call MindtwinManager.SetPostProcessingEnabled(false) like:

```javascript
[
    {
        "event": "Update",
        "id": "bridge",
        "data": {
            "targetTransform/component:MindtwinManager/method:SetPostProcessingEnabled": [false]
        }
    }
]
```

It looks at the C# metadata to know how to convert the method
parameters. It doesn't yet know how to handle ref, in, an out
parameters, though.

At some point I might add an "Invoke" event just for sending messages,
with an optional query callback for returning results, but this kinda
weird way of sending message using Update lets you send a bunch of
different messages to a bunch of different objects at once, so I've
just used that so far. I only rarely need to actually send messages --
most stuff I used it for just involved creating new objects, making
lots of updates and a few queries, and subscribing to lots of events.
I'm trying to keep it as simple as possible. But I'm eager to take
suggestions about how to improve it!

# **Installation**

+ `cd mtp_runtime/Assets`
+ `ln -s ../../bridge/Bridge`
+ `ln -s ../../bridge/StreamingAssets`
+ `ln -s ../../bridge/ThirdParty/UniWebServer`
+ `ln -s ../../bridge/ThirdParty/JsonDotNet`

+ Add the Bridge prefab into the scene. Point its targetTransform at the top level object you want to expose (i.e. MindtwinManager).

# ***Examples***

+ `curl http://127.0.0.1:7777/hello`
```javascript
{
  "hello": "world",
  "favorite_color": {
    "r": 0.0,
    "g": 1.0,
    "b": 0.0,
    "a": 1.0
}
````

+ `curl http://localhost:7777/receive_events`

```javascript
[{
  "event": "StartedUnity"
}]
````

+ `curl http://localhost:7777/receive_events`

```javascript
[]
````

+ `cat Json/test-send-events.json`

```javascript
[
    {
        "event": "StartedBridge"
    },
    {
        "event": "Log",
        "data": {
            "line": "Hello, world!"
        }
    },
    {
        "event": "Create",
        "data": {
            "id": "1",
            "prefab": "Prefabs/BridgeObject"
        }
    },
    {
        "event": "Create",
        "data": {
            "id": "2",
            "prefab": "Prefabs/PlainObject"
        }
    },
    {
        "event": "Create",
        "data": {
            "id": "3",
            "prefab": "Prefabs/UnityBridge"
        }
    },
    {
        "event": "Query",
        "id": "3",
        "data": {
            "callbackID": "1",
            "query": {
                "time": "time"
            }
        }
    },
    {
        "event": "Update",
        "id": "3",
        "data": {
            "timeScale": 0.5
        }
    }
]
```

+ `curl -X POST  -H "Content-Type: application/json" -d @Json/test-send-events.json http://localhost:7777/send_events`

```javascript
{
  "error": false,
  "message": "sent 7 events"
}
````

+ `curl http://localhost:7777/receive_events`

```javascript
[{
  "event": "Created",
  "id": "1"
},{
  "event": "Created",
  "id": "2"
},{
  "event": "Created",
  "id": "3"
},{
  "event": "Callback",
  "id": "1",
  "data": {
    "time": 50.3515
  }
}]
````
