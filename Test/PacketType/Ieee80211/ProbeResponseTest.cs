﻿/*
This file is part of PacketDotNet.

This Source Code Form is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/
/*
 * Copyright 2012 Alan Rushforth <alan.rushforth@gmail.com>
 */

using System.Net.NetworkInformation;
using NUnit.Framework;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using PacketDotNet.Utils;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Test.PacketType.Ieee80211
{
    [TestFixture]
    public class ProbeResponseTest
    {
        /// <summary>
        /// Test that parsing a probe response frame yields the proper field values
        /// </summary>
        [Test]
        public void Test_Constructor()
        {
            var dev = new CaptureFileReaderDevice(NUnitSetupClass.CaptureDirectory + "80211_probe_response_frame.pcap");
            dev.Open();
            PacketCapture c;
            dev.GetNextPacket(out c);
            var rawCapture = c.GetPacket();
            dev.Close();

            var p = Packet.ParsePacket(rawCapture.GetLinkLayers(), rawCapture.Data);
            var frame = (ProbeResponseFrame) p.PayloadPacket;

            Assert.AreEqual(0, frame.FrameControl.ProtocolVersion);
            Assert.AreEqual(FrameControlField.FrameSubTypes.ManagementProbeResponse, frame.FrameControl.SubType);
            Assert.IsFalse(frame.FrameControl.ToDS);
            Assert.IsFalse(frame.FrameControl.FromDS);
            Assert.IsFalse(frame.FrameControl.MoreFragments);
            Assert.IsTrue(frame.FrameControl.Retry);
            Assert.IsFalse(frame.FrameControl.PowerManagement);
            Assert.IsFalse(frame.FrameControl.MoreData);
            Assert.IsFalse(frame.FrameControl.Protected);
            Assert.IsFalse(frame.FrameControl.Order);
            Assert.AreEqual(314, frame.Duration.Field); //this need expanding on in the future
            Assert.AreEqual("0020008AB749", frame.DestinationAddress.ToString().ToUpper());
            Assert.AreEqual("00223FCD9C26", frame.SourceAddress.ToString().ToUpper());
            Assert.AreEqual("00223FCD9C26", frame.BssId.ToString().ToUpper());
            Assert.AreEqual(0, frame.SequenceControl.FragmentNumber);
            Assert.AreEqual(1468, frame.SequenceControl.SequenceNumber);

            Assert.AreEqual(0x0000047A44EF1DE0, frame.Timestamp);
            Assert.AreEqual(100, frame.BeaconInterval);

            Assert.IsTrue(frame.CapabilityInformation.IsEss);
            Assert.IsFalse(frame.CapabilityInformation.IsIbss);
            Assert.IsFalse(frame.CapabilityInformation.CfPollable);
            Assert.IsFalse(frame.CapabilityInformation.CfPollRequest);
            Assert.IsTrue(frame.CapabilityInformation.Privacy);
            Assert.IsFalse(frame.CapabilityInformation.ShortPreamble);
            Assert.IsFalse(frame.CapabilityInformation.Pbcc);
            Assert.IsFalse(frame.CapabilityInformation.ChannelAgility);
            Assert.IsTrue(frame.CapabilityInformation.ShortTimeSlot);
            Assert.IsFalse(frame.CapabilityInformation.DssOfdm);

            Assert.AreEqual(0x257202BE, frame.FrameCheckSequence);
            Assert.AreEqual(164, frame.FrameSize);
        }

        [Test]
        public void Test_Constructor_ConstructWithValues()
        {
            var ssidInfoElement = new InformationElement(InformationElement.ElementId.ServiceSetIdentity,
                                                         new byte[] { 0x68, 0x65, 0x6c, 0x6c, 0x6f });

            var vendorElement = new InformationElement(InformationElement.ElementId.VendorSpecific,
                                                       new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

            var frame = new ProbeResponseFrame(PhysicalAddress.Parse("111111111111"),
                                               PhysicalAddress.Parse("222222222222"),
                                               PhysicalAddress.Parse("333333333333"),
                                               new InformationElementList { ssidInfoElement, vendorElement })
            {
                FrameControl = { ToDS = false, FromDS = true, MoreFragments = true },
                Duration = { Field = 0x1234 },
                SequenceControl = { SequenceNumber = 0x77, FragmentNumber = 0x1 },
                CapabilityInformation = { IsEss = true, ChannelAgility = true }
            };

            frame.UpdateFrameCheckSequence();
            var fcs = frame.FrameCheckSequence;

            //serialize the frame into a byte buffer
            var bytes = frame.Bytes;
            var byteArraySegment = new ByteArraySegment(bytes);

            //create a new frame that should be identical to the original
            var recreatedFrame = MacFrame.ParsePacket(byteArraySegment) as ProbeResponseFrame;
            recreatedFrame.UpdateFrameCheckSequence();

            Assert.AreEqual(FrameControlField.FrameSubTypes.ManagementProbeResponse, recreatedFrame.FrameControl.SubType);
            Assert.IsFalse(recreatedFrame.FrameControl.ToDS);
            Assert.IsTrue(recreatedFrame.FrameControl.FromDS);
            Assert.IsTrue(recreatedFrame.FrameControl.MoreFragments);

            Assert.AreEqual(0x77, recreatedFrame.SequenceControl.SequenceNumber);
            Assert.AreEqual(0x1, recreatedFrame.SequenceControl.FragmentNumber);

            Assert.IsTrue(frame.CapabilityInformation.IsEss);
            Assert.IsTrue(frame.CapabilityInformation.ChannelAgility);

            Assert.AreEqual("111111111111", recreatedFrame.SourceAddress.ToString().ToUpper());
            Assert.AreEqual("222222222222", recreatedFrame.DestinationAddress.ToString().ToUpper());
            Assert.AreEqual("333333333333", recreatedFrame.BssId.ToString().ToUpper());

            Assert.AreEqual(ssidInfoElement, recreatedFrame.InformationElements[0]);
            Assert.AreEqual(vendorElement, recreatedFrame.InformationElements[1]);

            Assert.AreEqual(fcs, recreatedFrame.FrameCheckSequence);
        }

        [Test]
        public void Test_ConstructorWithCorruptBuffer()
        {
            //buffer is way too short for frame. We are just checking it doesn't throw
            byte[] corruptBuffer = { 0x01 };
            var frame = new ProbeResponseFrame(new ByteArraySegment(corruptBuffer));
            Assert.IsFalse(frame.FcsValid);
        }
    }
}