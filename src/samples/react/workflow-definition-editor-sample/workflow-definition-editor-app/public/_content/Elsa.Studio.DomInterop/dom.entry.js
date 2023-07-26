/******/ var __webpack_modules__ = ({

/***/ "./src/dom/element/click-element.ts":
/*!******************************************!*\
  !*** ./src/dom/element/click-element.ts ***!
  \******************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   clickElement: () => (/* binding */ clickElement)
/* harmony export */ });
/* harmony import */ var _get_element__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./get-element */ "./src/dom/element/get-element.ts");

function clickElement(elementOrQuerySelector) {
    const element = (0,_get_element__WEBPACK_IMPORTED_MODULE_0__.getElement)(elementOrQuerySelector);
    element.click();
}


/***/ }),

/***/ "./src/dom/element/get-bounding-client-rect.ts":
/*!*****************************************************!*\
  !*** ./src/dom/element/get-bounding-client-rect.ts ***!
  \*****************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   getBoundingClientRect: () => (/* binding */ getBoundingClientRect)
/* harmony export */ });
/* harmony import */ var _get_element__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./get-element */ "./src/dom/element/get-element.ts");

function getBoundingClientRect(elementOrQuerySelector) {
    const element = (0,_get_element__WEBPACK_IMPORTED_MODULE_0__.getElement)(elementOrQuerySelector);
    return element.getBoundingClientRect();
}


/***/ }),

/***/ "./src/dom/element/get-element.ts":
/*!****************************************!*\
  !*** ./src/dom/element/get-element.ts ***!
  \****************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   getElement: () => (/* binding */ getElement)
/* harmony export */ });
function getElement(elementOrQuerySelector) {
    return typeof elementOrQuerySelector === 'string' ? document.querySelector(elementOrQuerySelector) : elementOrQuerySelector;
}


/***/ }),

/***/ "./src/dom/element/get-visible-height.ts":
/*!***********************************************!*\
  !*** ./src/dom/element/get-visible-height.ts ***!
  \***********************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   getVisibleHeight: () => (/* binding */ getVisibleHeight)
/* harmony export */ });
/* harmony import */ var _get_element__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./get-element */ "./src/dom/element/get-element.ts");

function getVisibleHeight(elementOrQuerySelector) {
    const element = (0,_get_element__WEBPACK_IMPORTED_MODULE_0__.getElement)(elementOrQuerySelector);
    const rect = element.getBoundingClientRect();
    const windowHeight = window.innerHeight;
    if (rect.bottom > windowHeight) {
        const visibleHeight = windowHeight - rect.top;
        return visibleHeight > 0 ? visibleHeight : 0;
    }
    return rect.height;
}


/***/ }),

/***/ "./src/dom/element/index.ts":
/*!**********************************!*\
  !*** ./src/dom/element/index.ts ***!
  \**********************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   clickElement: () => (/* reexport safe */ _click_element__WEBPACK_IMPORTED_MODULE_0__.clickElement),
/* harmony export */   getBoundingClientRect: () => (/* reexport safe */ _get_bounding_client_rect__WEBPACK_IMPORTED_MODULE_1__.getBoundingClientRect),
/* harmony export */   getElement: () => (/* reexport safe */ _get_element__WEBPACK_IMPORTED_MODULE_2__.getElement),
/* harmony export */   getVisibleHeight: () => (/* reexport safe */ _get_visible_height__WEBPACK_IMPORTED_MODULE_3__.getVisibleHeight)
/* harmony export */ });
/* harmony import */ var _click_element__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./click-element */ "./src/dom/element/click-element.ts");
/* harmony import */ var _get_bounding_client_rect__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./get-bounding-client-rect */ "./src/dom/element/get-bounding-client-rect.ts");
/* harmony import */ var _get_element__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./get-element */ "./src/dom/element/get-element.ts");
/* harmony import */ var _get_visible_height__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./get-visible-height */ "./src/dom/element/get-visible-height.ts");






/***/ }),

/***/ "./src/dom/index.ts":
/*!**************************!*\
  !*** ./src/dom/index.ts ***!
  \**************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   clickElement: () => (/* reexport safe */ _element__WEBPACK_IMPORTED_MODULE_0__.clickElement),
/* harmony export */   getBoundingClientRect: () => (/* reexport safe */ _element__WEBPACK_IMPORTED_MODULE_0__.getBoundingClientRect),
/* harmony export */   getElement: () => (/* reexport safe */ _element__WEBPACK_IMPORTED_MODULE_0__.getElement),
/* harmony export */   getVisibleHeight: () => (/* reexport safe */ _element__WEBPACK_IMPORTED_MODULE_0__.getVisibleHeight)
/* harmony export */ });
/* harmony import */ var _element__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./element */ "./src/dom/element/index.ts");



/***/ })

/******/ });
/************************************************************************/
/******/ // The module cache
/******/ var __webpack_module_cache__ = {};
/******/ 
/******/ // The require function
/******/ function __webpack_require__(moduleId) {
/******/ 	// Check if module is in cache
/******/ 	var cachedModule = __webpack_module_cache__[moduleId];
/******/ 	if (cachedModule !== undefined) {
/******/ 		return cachedModule.exports;
/******/ 	}
/******/ 	// Create a new module (and put it into the cache)
/******/ 	var module = __webpack_module_cache__[moduleId] = {
/******/ 		// no module.id needed
/******/ 		// no module.loaded needed
/******/ 		exports: {}
/******/ 	};
/******/ 
/******/ 	// Execute the module function
/******/ 	__webpack_modules__[moduleId](module, module.exports, __webpack_require__);
/******/ 
/******/ 	// Return the exports of the module
/******/ 	return module.exports;
/******/ }
/******/ 
/************************************************************************/
/******/ /* webpack/runtime/define property getters */
/******/ (() => {
/******/ 	// define getter functions for harmony exports
/******/ 	__webpack_require__.d = (exports, definition) => {
/******/ 		for(var key in definition) {
/******/ 			if(__webpack_require__.o(definition, key) && !__webpack_require__.o(exports, key)) {
/******/ 				Object.defineProperty(exports, key, { enumerable: true, get: definition[key] });
/******/ 			}
/******/ 		}
/******/ 	};
/******/ })();
/******/ 
/******/ /* webpack/runtime/hasOwnProperty shorthand */
/******/ (() => {
/******/ 	__webpack_require__.o = (obj, prop) => (Object.prototype.hasOwnProperty.call(obj, prop))
/******/ })();
/******/ 
/******/ /* webpack/runtime/make namespace object */
/******/ (() => {
/******/ 	// define __esModule on exports
/******/ 	__webpack_require__.r = (exports) => {
/******/ 		if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 			Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 		}
/******/ 		Object.defineProperty(exports, '__esModule', { value: true });
/******/ 	};
/******/ })();
/******/ 
/************************************************************************/
var __webpack_exports__ = {};
// This entry need to be wrapped in an IIFE because it need to be isolated against other modules in the chunk.
(() => {
/*!********************!*\
  !*** ./src/dom.ts ***!
  \********************/
__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   clickElement: () => (/* reexport safe */ _dom___WEBPACK_IMPORTED_MODULE_0__.clickElement),
/* harmony export */   getBoundingClientRect: () => (/* reexport safe */ _dom___WEBPACK_IMPORTED_MODULE_0__.getBoundingClientRect),
/* harmony export */   getElement: () => (/* reexport safe */ _dom___WEBPACK_IMPORTED_MODULE_0__.getElement),
/* harmony export */   getVisibleHeight: () => (/* reexport safe */ _dom___WEBPACK_IMPORTED_MODULE_0__.getVisibleHeight)
/* harmony export */ });
/* harmony import */ var _dom___WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./dom/ */ "./src/dom/index.ts");


})();

var __webpack_exports__clickElement = __webpack_exports__.clickElement;
var __webpack_exports__getBoundingClientRect = __webpack_exports__.getBoundingClientRect;
var __webpack_exports__getElement = __webpack_exports__.getElement;
var __webpack_exports__getVisibleHeight = __webpack_exports__.getVisibleHeight;
export { __webpack_exports__clickElement as clickElement, __webpack_exports__getBoundingClientRect as getBoundingClientRect, __webpack_exports__getElement as getElement, __webpack_exports__getVisibleHeight as getVisibleHeight };

//# sourceMappingURL=dom.entry.js.map