// ============================================
// StickyNotes — Frontend Interactivity
// ============================================

document.addEventListener('DOMContentLoaded', () => {
    initColorPicker();
    initTypeToggle();
    initChecklistInteractions();
    initChecklistDragAndDrop();
    initAutoResizeTextarea();
    initNoteCardAnimations();
});

// --- Color Picker ---
function initColorPicker() {
    const picker = document.getElementById('color-picker');
    if (!picker) return;

    const swatches = picker.querySelectorAll('.color-swatch');
    const editorCard = document.querySelector('.editor-card');

    swatches.forEach(swatch => {
        swatch.addEventListener('click', () => {
            // Update active state
            swatches.forEach(s => {
                s.classList.remove('active');
                s.querySelector('.color-check')?.remove();
            });
            swatch.classList.add('active');

            // Add checkmark
            const check = document.createElement('span');
            check.className = 'color-check';
            check.textContent = '✓';
            swatch.appendChild(check);

            // Update card border color
            const color = swatch.querySelector('input').value;
            if (editorCard) {
                editorCard.style.setProperty('--note-color', color);
            }
        });
    });
}

// --- Type Toggle (Text / Checklist) ---
function initTypeToggle() {
    const toggle = document.getElementById('type-toggle');
    if (!toggle) return;

    const options = toggle.querySelectorAll('.toggle-option');
    const textEditor = document.getElementById('text-editor');
    const checklistEditor = document.getElementById('checklist-editor');

    options.forEach(option => {
        option.addEventListener('click', () => {
            options.forEach(o => o.classList.remove('active'));
            option.classList.add('active');

            const value = option.querySelector('input').value;
            if (value === 'Text') {
                textEditor?.classList.remove('hidden');
                checklistEditor?.classList.add('hidden');
            } else {
                textEditor?.classList.add('hidden');
                checklistEditor?.classList.remove('hidden');
            }
        });
    });
}

// --- Checklist Interactions ---
function initChecklistInteractions() {
    // Handle checkbox changes for strikethrough
    document.addEventListener('change', (e) => {
        if (e.target.closest('.checklist-check')) {
            const item = e.target.closest('.checklist-item');
            const input = item?.querySelector('.checklist-input');
            if (input) {
                input.classList.toggle('checked-text', e.target.checked);
            }
        }
    });

    // Handle Enter key to add new item
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && e.target.classList.contains('checklist-input')) {
            e.preventDefault();
            addChecklistItem();
        }
    });
}

// Add a new checklist item
function addChecklistItem() {
    const container = document.getElementById('checklist-items');
    if (!container) return;

    const index = container.querySelectorAll('.checklist-item').length;
    
    const item = document.createElement('div');
    item.className = 'checklist-item';
    item.setAttribute('data-index', index);
    item.setAttribute('draggable', 'true');
    item.innerHTML = `
        <div class="drag-handle" title="Drag to reorder">☰</div>
        <label class="checklist-check">
            <input type="checkbox" 
                   name="ChecklistItems[${index}].IsChecked" 
                   value="true" />
            <span class="checkmark"></span>
        </label>
        <input type="hidden" name="ChecklistItems[${index}].Id" value="0" />
        <input type="text" name="ChecklistItems[${index}].Content" 
               class="checklist-input" placeholder="Item text..." />
        <input type="hidden" name="ChecklistItems[${index}].Position" value="${index}" />
        <button type="button" class="btn-remove-item" onclick="removeChecklistItem(this)" title="Remove">×</button>
    `;

    container.appendChild(item);
    
    // Add event listeners for new item
    addDragListeners(item);

    // Focus the new input
    const newInput = item.querySelector('.checklist-input');
    if (newInput) {
        newInput.focus();
    }
}

// Remove a checklist item and re-index
function removeChecklistItem(button) {
    const item = button.closest('.checklist-item');
    if (!item) return;

    item.style.opacity = '0';
    item.style.transform = 'translateX(20px)';
    item.style.transition = 'all 0.2s ease-out';

    setTimeout(() => {
        item.remove();
        reindexChecklistItems();
    }, 200);
}

// Re-index checklist items after removal
function reindexChecklistItems() {
    const container = document.getElementById('checklist-items');
    if (!container) return;

    const items = container.querySelectorAll('.checklist-item');
    items.forEach((item, index) => {
        item.setAttribute('data-index', index);
        
        const checkbox = item.querySelector('.checklist-check input[type="checkbox"]');
        if (checkbox) checkbox.name = `ChecklistItems[${index}].IsChecked`;

        const idInput = item.querySelector('input[type="hidden"][name*=".Id"]');
        if (idInput) idInput.name = `ChecklistItems[${index}].Id`;

        const contentInput = item.querySelector('.checklist-input');
        if (contentInput) contentInput.name = `ChecklistItems[${index}].Content`;

        const positionInput = item.querySelector('input[type="hidden"][name*=".Position"]');
        if (positionInput) {
            positionInput.name = `ChecklistItems[${index}].Position`;
            positionInput.value = index;
        }
    });
}

// --- Auto-resize Textarea ---
function initAutoResizeTextarea() {
    const textarea = document.getElementById('note-content');
    if (!textarea) return;

    const resize = () => {
        textarea.style.height = 'auto';
        textarea.style.height = Math.max(300, textarea.scrollHeight) + 'px';
    };

    textarea.addEventListener('input', resize);
    resize(); // Initial size
}

// --- Staggered Card Animations ---
function initNoteCardAnimations() {
    const cards = document.querySelectorAll('.note-card');
    cards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.05}s`;
    });
}

// --- Drag and Drop Logic ---
function initChecklistDragAndDrop() {
    const items = document.querySelectorAll('.checklist-item');
    items.forEach(addDragListeners);
}

let draggedItem = null;

function addDragListeners(item) {
    item.addEventListener('dragstart', (e) => {
        draggedItem = item;
        setTimeout(() => item.classList.add('dragging'), 0);
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', ''); // Required for Firefox
    });

    item.addEventListener('dragend', () => {
        draggedItem = null;
        item.classList.remove('dragging');
        reindexChecklistItems();
    });

    item.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        const container = document.getElementById('checklist-items');
        if (!container || !draggedItem || draggedItem === item) return;

        // Determine drop position (above or below)
        const bounding = item.getBoundingClientRect();
        const offset = bounding.y + (bounding.height / 2);
        
        if (e.clientY > offset) {
            item.after(draggedItem);
        } else {
            item.before(draggedItem);
        }
    });
}
