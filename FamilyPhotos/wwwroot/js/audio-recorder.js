let mediaRecorder = null;
let chunks = [];
let startTime = 0;

export function getSupportedMimeType() {
    const types = ['audio/webm;codecs=opus', 'audio/webm', 'audio/mp4', 'audio/ogg;codecs=opus'];
    for (const type of types) {
        if (MediaRecorder.isTypeSupported(type)) return type;
    }
    return 'audio/webm';
}

export async function startRecording(dotNetRef) {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const mimeType = getSupportedMimeType();
        chunks = [];
        startTime = Date.now();

        mediaRecorder = new MediaRecorder(stream, { mimeType });

        mediaRecorder.ondataavailable = (e) => {
            if (e.data.size > 0) chunks.push(e.data);
        };

        mediaRecorder.onstop = async () => {
            const duration = (Date.now() - startTime) / 1000;
            const blob = new Blob(chunks, { type: mimeType });
            const buffer = await blob.arrayBuffer();
            const bytes = new Uint8Array(buffer);

            // Convert to base64
            let binary = '';
            const chunkSize = 8192;
            for (let i = 0; i < bytes.length; i += chunkSize) {
                const slice = bytes.subarray(i, i + chunkSize);
                binary += String.fromCharCode.apply(null, slice);
            }
            const base64 = btoa(binary);

            // Clean up media stream
            stream.getTracks().forEach(track => track.stop());

            const baseMime = mimeType.split(';')[0];
            await dotNetRef.invokeMethodAsync('OnRecordingComplete', base64, baseMime, duration);
        };

        mediaRecorder.start(250); // Collect chunks every 250ms
        return true;
    } catch (err) {
        console.error('Failed to start recording:', err);
        return false;
    }
}

export function stopRecording() {
    if (mediaRecorder && mediaRecorder.state === 'recording') {
        mediaRecorder.stop();
        return true;
    }
    return false;
}

export function isRecording() {
    return mediaRecorder?.state === 'recording';
}
