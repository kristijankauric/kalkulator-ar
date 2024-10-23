const header = document.getElementById('header')
const menuButtons = document.getElementById('menu-buttons')
const backButton = document.getElementById('button-back')
const navigation = document.getElementById('navigation')
const title = document.getElementById('title')
const description = document.getElementById('description')
const placeButton = document.getElementById('place-button')
const resetButton = document.getElementById('reset-button')

var groups
var selectedItem
var selectedGroup
function setSelectedItem(item) { selectedItem = item }
function getSelectedItem() { return selectedItem }
function setSelectedGroup(group) { selectedGroup = group }
function getSelectedGroup() { return selectedGroup }

function selectCategory(group) {
    menuButtons.clear()
    group.items?.forEach(createItemCards)
    group.description && (description.textContent = group.description)
    title.textContent = group.label
    backButton._show(true)
    backButton.onclick = backButtonHandleHome;
    selectedGroup = group
}

function createItemCards(item) {
    menuButtons.appendChild(createItemCard(item))
}

function createItemCard(item) {
    let itemCard = document.createElement('div')
    let titleStrip = createCardTitleStrip(item)
    itemCard.classList.add('item-card')
    itemCard.style.backgroundImage = `url('${item.image}')`
    itemCard.onclick = (e) => selectCard(e, item)
    itemCard.appendChild(titleStrip)
    return itemCard

    function createCardTitleStrip(item) {
        let titleStrip = document.createElement('div')
        titleStrip.classList.add('title')
        titleStrip.textContent = item.title
        return titleStrip
    }
}

function selectCard(e, item) {
    resetSelectedCard(getSelectedItem())
    setSelectedItem(item)
    let playButtonOverlay = create('div', 'card-overlay')
    let playButton = create('span', 'card-button')
    playButton.textContent = "Pokreni"
    playButtonOverlay.appendChild(playButton)
    
    e.currentTarget.classList.add('selected')
    e.currentTarget.appendChild(playButtonOverlay)
    e.currentTarget.onclick = (e) => startContent(e, item)
    title.textContent = item.title
    description.textContent = item.description
}

function startContent(e, item) {
    if (item.story) {
        window.unityInstance.SendMessage(
            'Jankec Character', 
            'StartAnimation', 
            item.story
        )

        menuButtons.clear()

        if (item.transcript) {
            description.textContent = item.transcript
        }

        backButton._show(true)
        backButton.onclick = backButtonHandleGroup
    }
    else if (item.link) {
        window.open(item.link, '_self')
    }
}

function resetSelectedCard(oldItem) {
    let selected = document.querySelector('.selected')
    if (selected) {
        selected.classList.remove('selected')
        selected.onclick = (ev) => selectCard(ev, oldItem)
        document.querySelector('.card-overlay').remove()
    }
}

function createMenuButton(group) {
    let menuButtonWrapper = create('div', 'menu-button-wrapper')
    let menuButtonLabel = create('div', 'menu-button')

    if (group.image) {
        let menuButtonIcon = create('img', 'menu-button-img')
        menuButtonIcon.src = group.image // add icon
        menuButtonWrapper.appendChild(menuButtonIcon)
    } 
    
    menuButtonLabel.textContent = group.label // add text label
    
    menuButtonWrapper.appendChild(menuButtonLabel)
    menuButtonWrapper.addEventListener('click', 
        () => selectCategory(group))

    menuButtons.appendChild(menuButtonWrapper)
}

function populateMainMenu(groups) {
    menuButtons.clear()
    backButton._show(false)
    title.textContent = 'Odaberi sadržaj:'
    groups?.forEach(createMenuButton)
}

function backButtonHandleHome() {
    description.clear()
    populateMainMenu(groups)
}

function backButtonHandleGroup() {
    let group = getSelectedGroup()
    selectCategory(group)
}

function showUI() {
    navigation._show(true)
}

fetch('jankec.json')
    .then(res => res.json())
    .then(data => {
        groups = data.groups
        populateMainMenu(groups)
    })

function create(tag, _class) {
    let element = document.createElement(tag)
    element.classList.add(_class)
    return element
}

function resetOrigin() {
    navigation._show(false)
    placeButton._show(true)
    resetButton._show(false)
    window.unityInstance.SendMessage('WorldTracker', 'ResetOrigin')
}

function placeOrigin() {
    placeButton._show(false)
    resetButton._show(true)
    window.unityInstance.SendMessage('WorldTracker', 'PlaceOrigin')
}

function showElement(element, toShow) {
    element.classList.toggle('d-none', !toShow)
}

HTMLElement.prototype._show = function(toShow) {
    this.classList.toggle('d-none', !toShow)
    return this
}

HTMLElement.prototype.clear = function() {
    this.innerHTML = ''
    return this
}