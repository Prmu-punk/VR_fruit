using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace WebcamXR
{
    public class WebcamTrackingReceiver : MonoBehaviour
    {
        [SerializeField] private int port = 7777;
        [SerializeField] private float maxPacketAgeSeconds = 0.5f;
        [SerializeField] private bool logPackets = false;

        private UdpClient client;
        private IPEndPoint remoteEndPoint;
        private TrackingFrameV1 latestFrame = new TrackingFrameV1();
        private float lastPacketRealtime = -1f;

        public TrackingFrameV1 LatestFrame => latestFrame;
        public bool HasFreshFrame => lastPacketRealtime >= 0f && SecondsSinceLastPacket <= maxPacketAgeSeconds;
        public float SecondsSinceLastPacket => lastPacketRealtime < 0f ? float.PositiveInfinity : Time.realtimeSinceStartup - lastPacketRealtime;

        private void OnEnable()
        {
            TryOpenSocket();
        }

        private void OnDisable()
        {
            CloseSocket();
        }

        private void Update()
        {
            if (client == null)
                return;

            while (client.Available > 0)
            {
                try
                {
                    byte[] bytes = client.Receive(ref remoteEndPoint);
                    string json = Encoding.UTF8.GetString(bytes);
                    TrackingFrameV1 frame = JsonUtility.FromJson<TrackingFrameV1>(json);

                    if (frame == null || frame.version != 1)
                        continue;

                    latestFrame = frame;
                    lastPacketRealtime = Time.realtimeSinceStartup;

                    if (logPackets)
                        Debug.Log(json, this);
                }
                catch (SocketException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Failed to parse webcam tracking packet: {exception.Message}", this);
                    break;
                }
            }
        }

        private void TryOpenSocket()
        {
            try
            {
                remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                client = new UdpClient(port);
                client.Client.ReceiveTimeout = 0;
                client.Client.Blocking = false;
                Debug.Log($"WebcamTrackingReceiver listening on UDP port {port}.", this);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to open webcam tracking receiver on port {port}: {exception.Message}", this);
            }
        }

        private void CloseSocket()
        {
            if (client == null)
                return;

            client.Close();
            client = null;
        }
    }
}
