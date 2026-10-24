window.webGwent = window.webGwent || {};

window.webGwent.scrollToId = function (id) {
  const el = document.getElementById(id);
  if (!el) return false;
  el.scrollIntoView({ behavior: "smooth", block: "start" });
  return true;
};

window.webGwent.rememberHandCard = function (cardId) {
  const el = document.querySelector(`.hand-panel [data-card-id="${cardId}"]`);
  if (!el) {
    window.webGwent._fromRect = null;
    return;
  }
  const r = el.getBoundingClientRect();
  window.webGwent._fromRect = {
    left: r.left,
    top: r.top,
    width: r.width,
    height: r.height,
    html: el.outerHTML,
    fromHand: true
  };
};

function rectOf(el, fallback) {
  if (!el) return fallback;
  const r = el.getBoundingClientRect();
  return { left: r.left, top: r.top, width: r.width, height: r.height };
}

function opponentOrigin(to) {
  const hand = document.querySelector('.meta-col.opponent .meta-hand');
  const deck = document.querySelector('[data-pile="opp-deck"]');
  const meta = document.querySelector('.meta-col.opponent');
  const base = rectOf(hand || deck || meta, {
    left: Math.max(16, to.left - 80),
    top: Math.max(24, to.top - 40),
    width: to.width,
    height: to.height
  });
  // Card-back ghost — never reuse player's hand HTML
  return {
    left: base.left + Math.max(0, (base.width - to.width) / 2),
    top: base.top + Math.max(0, (base.height - to.height) / 2),
    width: to.width,
    height: to.height,
    html: '<div class="gwent-card fly-card-back" aria-hidden="true"></div>',
    fromOpponent: true
  };
}

function playerOrigin(to) {
  const remembered = window.webGwent._fromRect;
  window.webGwent._fromRect = null;
  if (remembered && remembered.fromHand) return remembered;

  const hand = document.querySelector('.hand-panel .card-row.hand');
  const r = rectOf(hand, {
    left: to.left,
    top: window.innerHeight - 120,
    width: to.width,
    height: to.height
  });
  return {
    left: r.left + Math.max(0, (r.width - to.width) / 2),
    top: r.top,
    width: to.width,
    height: to.height,
    html: null
  };
}

window.webGwent.animatePlayedCard = function (cardId, fromOpponent) {
  const target = document.querySelector(`.battlefield [data-card-id="${cardId}"]`);
  if (!target) return;

  const to = target.getBoundingClientRect();

  // Opponent must never use the player's remembered hand rect.
  let from;
  if (fromOpponent) {
    window.webGwent._fromRect = null;
    from = opponentOrigin(to);
  } else {
    from = playerOrigin(to);
  }

  const ghost = document.createElement('div');
  ghost.className = 'card-fly-ghost';
  ghost.innerHTML = from.html || target.outerHTML;
  const card = ghost.firstElementChild || ghost;
  card.style.width = `${from.width || to.width}px`;
  card.style.height = `${from.height || to.height}px`;
  card.style.margin = '0';
  card.style.pointerEvents = 'none';

  const startRotate = fromOpponent ? -18 : 8;
  ghost.style.cssText = [
    'position:fixed',
    `left:${from.left}px`,
    `top:${from.top}px`,
    `width:${from.width || to.width}px`,
    `height:${from.height || to.height}px`,
    'z-index:200',
    'pointer-events:none',
    'margin:0',
    `transform:translate(0,0) scale(0.92) rotate(${startRotate}deg)`,
    'transition:transform .52s cubic-bezier(.2,.8,.2,1), opacity .52s ease',
    'filter:drop-shadow(0 12px 18px rgba(0,0,0,.55))',
    'will-change:transform',
    'opacity:1'
  ].join(';');

  document.body.appendChild(ghost);
  target.classList.add('fly-arrive-hidden');

  const dx = to.left - from.left;
  const dy = to.top - from.top;
  const sx = (to.width || from.width) / (from.width || to.width || 1);
  const sy = (to.height || from.height) / (from.height || to.height || 1);

  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      ghost.style.transform = `translate(${dx}px, ${dy}px) scale(${sx}, ${sy}) rotate(0deg)`;
    });
  });

  window.setTimeout(() => {
    ghost.remove();
    target.classList.remove('fly-arrive-hidden');
    target.classList.add('fly-arrive-pop');
    window.setTimeout(() => target.classList.remove('fly-arrive-pop'), 420);
  }, 540);
};

/** Fly every board card into its side's discard pile. Returns a Promise. */
window.webGwent.sweepBoardToDiscard = function () {
  return new Promise((resolve) => {
    const youPile = document.querySelector('[data-pile="you-discard"]');
    const oppPile = document.querySelector('[data-pile="opp-discard"]');
    const youCards = [...document.querySelectorAll('.field-col.you .combat-row .gwent-card')];
    const oppCards = [...document.querySelectorAll('.field-col.opponent .combat-row .gwent-card')];

    const fly = (el, pile, delay) => {
      if (!el || !pile) return;
      const from = el.getBoundingClientRect();
      const to = pile.getBoundingClientRect();
      const ghost = el.cloneNode(true);
      ghost.classList.add('card-fly-ghost');
      ghost.style.cssText = [
        'position:fixed',
        `left:${from.left}px`,
        `top:${from.top}px`,
        `width:${from.width}px`,
        `height:${from.height}px`,
        'z-index:220',
        'pointer-events:none',
        'margin:0',
        'transition:transform .55s cubic-bezier(.25,.8,.25,1), opacity .55s ease',
        'filter:drop-shadow(0 10px 14px rgba(0,0,0,.5))',
        `transition-delay:${delay}ms`
      ].join(';');
      document.body.appendChild(ghost);
      el.style.opacity = '0';

      const dx = to.left + to.width * 0.15 - from.left;
      const dy = to.top + to.height * 0.1 - from.top;

      requestAnimationFrame(() => {
        requestAnimationFrame(() => {
          ghost.style.transform = `translate(${dx}px, ${dy}px) scale(0.45) rotate(${delay % 2 === 0 ? -12 : 10}deg)`;
          ghost.style.opacity = '0.15';
        });
      });

      window.setTimeout(() => ghost.remove(), 600 + delay);
    };

    youCards.forEach((c, i) => fly(c, youPile, i * 45));
    oppCards.forEach((c, i) => fly(c, oppPile, i * 45));

    const total = Math.max(youCards.length, oppCards.length);
    const wait = total === 0 ? 120 : 650 + total * 45;
    window.setTimeout(() => resolve(true), wait);
  });
};
