const SipAudio = {
    _pc: null,

    async createOffer() {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });

        this._pc = new RTCPeerConnection({
            iceServers: [{ urls: 'stun:stun.l.google.com:19302' }]
        });

        stream.getTracks().forEach(track => this._pc.addTrack(track, stream));

        this._pc.ontrack = (e) => {
            let audio = document.getElementById('sip-remote-audio');
            if (!audio) {
                audio = document.createElement('audio');
                audio.id = 'sip-remote-audio';
                audio.autoplay = true;
                document.body.appendChild(audio);
            }
            audio.srcObject = e.streams[0];
        };

        const offer = await this._pc.createOffer();
        await this._pc.setLocalDescription(offer);

        // Wait for ICE gathering to complete (timeout after 10 s to avoid hanging indefinitely)
        await new Promise((resolve, reject) => {
            if (this._pc.iceGatheringState === 'complete') { resolve(); return; }
            const timeout = setTimeout(() => reject(new Error('ICE gathering timed out')), 10000);
            this._pc.onicegatheringstatechange = () => {
                if (this._pc.iceGatheringState === 'complete') {
                    clearTimeout(timeout);
                    resolve();
                }
            };
        });

        return this._pc.localDescription.sdp;
    },

    async applyAnswer(remoteSdp) {
        await this._pc.setRemoteDescription(
            new RTCSessionDescription({ type: 'answer', sdp: remoteSdp })
        );
    },

    hangUp() {
        if (this._pc) {
            this._pc.close();
            this._pc = null;
        }
        const audio = document.getElementById('sip-remote-audio');
        if (audio) { audio.srcObject = null; }
    }
};

window.SipAudio = SipAudio;
