(() => {
  const AudioCtx = window.AudioContext || window.webkitAudioContext;
  let ctx = null;

  function ensureCtx() {
    if (!AudioCtx) return null;
    if (!ctx) ctx = new AudioCtx();
    if (ctx.state === 'suspended') ctx.resume();
    return ctx;
  }

  function tone(freq, dur, type, gain) {
    const c = ensureCtx();
    if (!c) return;
    const t0 = c.currentTime;
    const o = c.createOscillator();
    const g = c.createGain();
    o.type = type || 'sine';
    o.frequency.value = freq;
    g.gain.setValueAtTime(gain ?? 0.04, t0);
    g.gain.exponentialRampToValueAtTime(0.001, t0 + dur);
    o.connect(g);
    g.connect(c.destination);
    o.start(t0);
    o.stop(t0 + dur);
  }

  function flash(cls) {
    const board = document.querySelector('.board');
    if (!board) return;
    board.classList.remove(cls);
    // reflow
    void board.offsetWidth;
    board.classList.add(cls);
    setTimeout(() => board.classList.remove(cls), 700);
  }

  window.webGwentFx = {
    playCard() { tone(330, 0.08, 'triangle', 0.03); },
    weather() {
      tone(180, 0.25, 'sawtooth', 0.025);
      setTimeout(() => tone(140, 0.3, 'sine', 0.02), 80);
      flash('fx-weather');
    },
    scorch() {
      tone(90, 0.18, 'square', 0.035);
      setTimeout(() => tone(220, 0.12, 'sawtooth', 0.03), 60);
      flash('fx-scorch');
    },
    pass() { tone(240, 0.06, 'sine', 0.02); },
    unlock() { ensureCtx(); }
  };

  document.addEventListener('pointerdown', () => window.webGwentFx.unlock(), { once: true });
})();
