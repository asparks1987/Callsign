const search = document.querySelector('[data-doc-search]');
if (search) {
  const cards = Array.from(document.querySelectorAll('[data-doc-card]'));
  search.addEventListener('input', () => {
    const q = search.value.trim().toLowerCase();
    cards.forEach(card => {
      const haystack = card.innerText.toLowerCase();
      card.style.display = haystack.includes(q) ? '' : 'none';
    });
  });
}
