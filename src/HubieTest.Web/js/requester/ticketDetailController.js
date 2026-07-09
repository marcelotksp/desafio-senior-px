/* =====================================================================
   ticketDetailController (REQUESTER) — TODO (candidate area)
   Ticket detail + interaction thread + attachments. The requester can also
   reply (interact) and attach files.
   ===================================================================== */
angular.module('hubieTest').controller('ticketDetailController',
    ['$scope', '$stateParams', 'apiService', 'modalService',
    function ($scope, $stateParams, apiService, modalService) {

        $scope.ticketId = parseInt($stateParams.id, 10);
        $scope.ticket = null;
        $scope.interactions = [];
        $scope.attachments = [];
        $scope.file = null;
        $scope.loading = false;
        $scope.sending = false;
        $scope.uploading = false;
        $scope.changing = false;

        $scope.onFileChange = function (element) {
            $scope.$apply(function () {
                $scope.file = element.files[0] || null;
            });
        };

        $scope.load = function () {
            $scope.loading = true;
            apiService.getTicket($scope.ticketId)
                .then(function (res) {
                    $scope.ticket = res.data;
                    return apiService.listInteractions($scope.ticketId);
                })
                .then(function (res) {
                    $scope.interactions = res.data || [];
                    return apiService.listAttachments($scope.ticketId);
                })
                .then(function (res) { $scope.attachments = res.data || []; })
                .catch(function (err) {
                    modalService.show((err.data || {}).error || 'Failed to load ticket.');
                })
                .finally(function () { $scope.loading = false; });
        };

        $scope.reply = function () {
            var el  = document.getElementById('reply-textarea');
            var msg = el ? el.value : '';
            if (!msg || !msg.trim()) {
                modalService.show('Please write a message before sending.', 'Validation');
                return;
            }
            $scope.sending = true;
            apiService.addInteraction($scope.ticketId, msg)
                .then(function (res) {
                    $scope.interactions.push(res.data);
                    if (el) el.value = '';
                })
                .catch(function (err) {
                    modalService.show((err.data || {}).error || 'Failed to send message.');
                })
                .finally(function () { $scope.sending = false; });
        };

        $scope.attach = function () {
            if (!$scope.file) return;
            $scope.uploading = true;
            apiService.uploadAttachment($scope.ticketId, $scope.file)
                .then(function (res) {
                    $scope.attachments.unshift(res.data);
                    $scope.file = null;
                    var el = document.getElementById('file-input');
                    if (el) el.value = '';
                })
                .catch(function (err) {
                    modalService.show((err.data || {}).error || 'Upload failed.');
                })
                .finally(function () { $scope.uploading = false; });
        };

        $scope.downloadUrl = function (a) {
            return apiService.downloadUrl($scope.ticketId, a.ATTACHMENT_ID);
        };

        $scope.closeTicket = function () {
            $scope.changing = true;
            apiService.changeStatus($scope.ticketId, 'CLOSED')
                .then(function () { return apiService.getTicket($scope.ticketId); })
                .then(function (res) { $scope.ticket = res.data; })
                .catch(function (err) {
                    modalService.show((err.data || {}).error || 'Failed to close ticket.');
                })
                .finally(function () { $scope.changing = false; });
        };

        $scope.load();
    }
]);
