﻿/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System.Collections.Generic;
using UnityEngine.Assertions;

public class AirXRServerStreamHandover {
    public class Streams {
        public Streams(int playerID, AirXRServerMediaStream mediaStream, AirXRServerInputStream inputStream) {
            Assert.IsNotNull(inputStream.owner);

            this.playerID = playerID;
            this.mediaStream = mediaStream;
            this.inputStream = inputStream;
        }

        public int playerID { get; private set; }
        public AirXRServerMediaStream mediaStream { get; private set; }
        public AirXRServerInputStream inputStream { get; private set; }

        public void OnHandedOver() {
            inputStream.owner = null;
        }

        public void Destroy() {
            mediaStream.Destroy();
        }
    }

    private static List<Streams> _handedOverStreams = new List<Streams>();

    public static void HandOverStreamsForNextScene(Streams streams) {
        streams.OnHandedOver();
        _handedOverStreams.Add(streams);
    }

    public static void TakeAllStreamsHandedOverInPrevScene(List<Streams> result) {
        result.AddRange(_handedOverStreams);
        _handedOverStreams.Clear();
    }
}
