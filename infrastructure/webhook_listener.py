from http.server import BaseHTTPRequestHandler, HTTPServer
import subprocess

class WebhookHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        # Webhookが届いたら ci.sh を実行する
        print("\n🔔 Webhook received! Triggering CI/CD...")
        subprocess.run(["./ci.sh"])
        
        self.send_response(200)
        self.end_headers()
        self.wfile.write(b"OK")

print("📡 Webhook Listener started on port 9000...")
HTTPServer(('localhost', 9000), WebhookHandler).serve_forever()
