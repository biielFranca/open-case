window.openCaseHints = {
    speak(text) {
        if (!("speechSynthesis" in window) || !text) {
            return;
        }

        window.speechSynthesis.cancel();

        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = "pt-BR";
        utterance.rate = 0.92;
        utterance.pitch = 0.82;
        window.speechSynthesis.speak(utterance);
    }
};
