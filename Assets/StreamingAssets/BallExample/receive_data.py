"""
JSBSim UDP Socket Receiver
Receives and displays simulation data in real-time
"""
import socket
import struct

# UDP socket setup
UDP_IP = "127.0.0.1"
UDP_PORT = 5138

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind((UDP_IP, UDP_PORT))
sock.settimeout(30)  # 30 second timeout

print("=" * 70)
print("JSBSim Real-Time Data Receiver")
print(f"Listening on {UDP_IP}:{UDP_PORT}")
print("=" * 70)
print(f"{'Time (s)':>10} {'Altitude (ft)':>15} {'V-North':>12} {'V-East':>12} {'V-Down':>12}")
print("-" * 70)

try:
    while True:
        data, addr = sock.recvfrom(1024)
        # JSBSim sends comma-separated text data
        try:
            values = data.decode('utf-8').strip().split(',')
            if len(values) >= 5:
                time_s = float(values[0])
                alt_ft = float(values[1])
                v_n = float(values[2])
                v_e = float(values[3])
                v_d = float(values[4])
                print(f"{time_s:>10.2f} {alt_ft:>15.2f} {v_n:>12.2f} {v_e:>12.2f} {v_d:>12.2f}")
        except (ValueError, IndexError):
            # Try binary format (doubles)
            if len(data) >= 40:  # 5 doubles * 8 bytes
                values = struct.unpack('5d', data[:40])
                print(f"{values[0]:>10.2f} {values[1]:>15.2f} {values[2]:>12.2f} {values[3]:>12.2f} {values[4]:>12.2f}")
            else:
                print(f"Raw: {data}")
except socket.timeout:
    print("\nSimulation complete (timeout reached)")
except KeyboardInterrupt:
    print("\nStopped by user")
finally:
    sock.close()
    print("Socket closed")
