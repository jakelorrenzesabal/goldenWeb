// Global Modal & Social Sharing Handlers for Golden Success College

let previousUrl = window.location.href;

function openEventModal(eventId) {
    if (!eventId) return;
    previousUrl = window.location.href;
    const detailUrl = `${window.location.origin}/Events/Details/${eventId}`;

    fetch(`/Events/GetJson/${eventId}`)
        .then(res => {
            if (!res.ok) throw new Error("Event not found");
            return res.json();
        })
        .then(data => {
            document.getElementById('evtModalTitle').textContent = data.title;
            document.getElementById('evtModalCategory').textContent = data.category || 'Event';
            document.getElementById('evtModalDate').textContent = data.formattedDate || data.eventDate;
            document.getElementById('evtModalTime').textContent = `${data.startTime} - ${data.endTime}`;
            document.getElementById('evtModalLocation').textContent = data.location || 'Main Campus';
            document.getElementById('evtModalOrganizer').textContent = data.organizer || 'College Admin';
            document.getElementById('evtModalDescription').textContent = data.description || 'No description provided.';

            const imgContainer = document.getElementById('evtModalImageContainer');
            const imgEl = document.getElementById('evtModalImage');
            if (data.hasImage && data.imageUrl) {
                imgEl.src = data.imageUrl;
                imgContainer.classList.remove('d-none');
            } else {
                imgContainer.classList.add('d-none');
                imgEl.src = '';
            }

            document.getElementById('evtModalFullLink').href = `/Events/Details/${eventId}`;

            const fbShareUrl = `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(detailUrl)}`;
            const fbBtn = document.getElementById('evtModalFbBtn');
            fbBtn.onclick = function () {
                window.open(fbShareUrl, 'fbShareWindow', 'width=600,height=500');
            };

            const copyBtn = document.getElementById('evtModalCopyBtn');
            copyBtn.onclick = function () {
                navigator.clipboard.writeText(detailUrl).then(() => {
                    alert('Event URL copied to clipboard:\n' + detailUrl);
                });
            };

            const modalEl = document.getElementById('gscEventModal');
            const bsModal = new bootstrap.Modal(modalEl);

            // Update browser URL
            if (window.history && window.history.pushState) {
                window.history.pushState({ modal: 'event', id: eventId }, '', `/Events/Details/${eventId}`);
            }

            modalEl.addEventListener('hidden.bs.modal', function onHide() {
                modalEl.removeEventListener('hidden.bs.modal', onHide);
                if (window.history && window.history.pushState && window.location.pathname.includes('/Events/Details/')) {
                    window.history.pushState(null, '', previousUrl);
                }
            });

            bsModal.show();
        })
        .catch(err => {
            console.error(err);
            window.location.href = `/Events/Details/${eventId}`;
        });
}

function openBulletinModal(bulletinId) {
    if (!bulletinId) return;
    previousUrl = window.location.href;
    const detailUrl = `${window.location.origin}/Bulletins/Details/${bulletinId}`;

    fetch(`/Bulletins/GetJson/${bulletinId}`)
        .then(res => {
            if (!res.ok) throw new Error("Bulletin not found");
            return res.json();
        })
        .then(data => {
            document.getElementById('bulModalTitle').textContent = data.title;
            const priorityBadge = document.getElementById('bulModalPriority');
            priorityBadge.textContent = `${data.priority || 'Normal'} Priority`;
            priorityBadge.className = `badge me-2 ${data.priority === 'High' ? 'bg-danger' : 'bg-primary'}`;

            document.getElementById('bulModalCategory').textContent = data.category || 'General Notice';
            document.getElementById('bulModalAuthor').textContent = data.author || 'Student Affairs';
            document.getElementById('bulModalDate').textContent = data.formattedDate || data.publishDate;
            document.getElementById('bulModalContent').textContent = data.content;

            const imgContainer = document.getElementById('bulModalImageContainer');
            const imgEl = document.getElementById('bulModalImage');
            if (data.hasImage && data.imageUrl) {
                imgEl.src = data.imageUrl;
                imgContainer.classList.remove('d-none');
            } else {
                imgContainer.classList.add('d-none');
                imgEl.src = '';
            }

            document.getElementById('bulModalFullLink').href = `/Bulletins/Details/${bulletinId}`;

            const fbShareUrl = `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(detailUrl)}`;
            const fbBtn = document.getElementById('bulModalFbBtn');
            fbBtn.onclick = function () {
                window.open(fbShareUrl, 'fbShareWindow', 'width=600,height=500');
            };

            const copyBtn = document.getElementById('bulModalCopyBtn');
            copyBtn.onclick = function () {
                navigator.clipboard.writeText(detailUrl).then(() => {
                    alert('Bulletin URL copied to clipboard:\n' + detailUrl);
                });
            };

            const modalEl = document.getElementById('gscBulletinModal');
            const bsModal = new bootstrap.Modal(modalEl);

            // Update browser URL
            if (window.history && window.history.pushState) {
                window.history.pushState({ modal: 'bulletin', id: bulletinId }, '', `/Bulletins/Details/${bulletinId}`);
            }

            modalEl.addEventListener('hidden.bs.modal', function onHide() {
                modalEl.removeEventListener('hidden.bs.modal', onHide);
                if (window.history && window.history.pushState && window.location.pathname.includes('/Bulletins/Details/')) {
                    window.history.pushState(null, '', previousUrl);
                }
            });

            bsModal.show();
        })
        .catch(err => {
            console.error(err);
            window.location.href = `/Bulletins/Details/${bulletinId}`;
        });
}

