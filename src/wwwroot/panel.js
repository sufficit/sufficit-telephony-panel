window.telephonyPanel = (() => {
    let audio;
    let lastAlert = 0;
    let boardObserver;
    let fitBoard;
    const play = async () => {
        if (!audio || document.hidden || Date.now() - lastAlert < 15000) return;
        await audio.resume();
        lastAlert = Date.now();
        const tone = audio.createOscillator();
        const gain = audio.createGain();
        tone.type = 'sine';
        tone.frequency.setValueAtTime(660, audio.currentTime);
        tone.frequency.setValueAtTime(880, audio.currentTime + .12);
        gain.gain.setValueAtTime(.0001, audio.currentTime);
        gain.gain.exponentialRampToValueAtTime(.08, audio.currentTime + .02);
        gain.gain.exponentialRampToValueAtTime(.0001, audio.currentTime + .3);
        tone.connect(gain); gain.connect(audio.destination);
        tone.start(); tone.stop(audio.currentTime + .32);
        tone.onended = () => { tone.disconnect(); gain.disconnect(); };
    };
    return {
        fitBoard: () => {
            boardObserver?.disconnect();
            if (fitBoard) window.removeEventListener('resize', fitBoard);
            const panel = document.querySelector('.live-panel.board-fill');
            if (!panel) return;
            fitBoard = () => {
                const grid = panel.querySelector('.board-grid');
                if (!grid) return;
                const height = Math.max(200, window.innerHeight - grid.getBoundingClientRect().top - window.scrollY - 24);
                const value = `${Math.floor(height)}px`;
                if (panel.style.getPropertyValue('--board-available-height') !== value) panel.style.setProperty('--board-available-height', value);
            };
            boardObserver = new ResizeObserver(fitBoard);
            boardObserver.observe(panel);
            window.addEventListener('resize', fitBoard);
            fitBoard();
        },
        stopFitBoard: () => { boardObserver?.disconnect(); if (fitBoard) window.removeEventListener('resize', fitBoard); },
        fullscreen: async id => {
            try {
                if (document.fullscreenElement) await document.exitFullscreen();
                else {
                    const board = document.getElementById(id);
                    if (!board?.requestFullscreen) return false;
                    await board.requestFullscreen();
                }
                return true;
            } catch { return false; }
        },
        language: () => { try { return localStorage.getItem('telephony-panel-language') === 'pt-BR' ? 'pt-BR' : 'en'; } catch { return 'en'; } },
        setLanguage: value => {
            const language = value === 'pt-BR' ? 'pt-BR' : 'en';
            document.documentElement.lang = language;
            try { localStorage.setItem('telephony-panel-language', language); } catch {}
        },
        dark: () => {
            // An explicit choice wins; otherwise follow the operating system on first visit.
            try { const saved = localStorage.getItem('telephony-panel-theme'); if (saved) return saved === 'dark'; } catch {}
            return window.matchMedia?.('(prefers-color-scheme: dark)').matches === true;
        },
        theme: dark => { try { localStorage.setItem('telephony-panel-theme', dark ? 'dark' : 'light'); } catch {} },
        enableSound: async () => { audio ??= new (window.AudioContext || window.webkitAudioContext)(); await play(); },
        alert: play
    };
})();
