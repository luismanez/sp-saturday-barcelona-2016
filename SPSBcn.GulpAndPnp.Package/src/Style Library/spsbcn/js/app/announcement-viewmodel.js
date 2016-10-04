var AnnouncementViewModel = function () {

    self = this;

    // map array of passed in announcements to an observableArray of Announcements objects
    self.announcements = ko.observableArray();

    $pnp.sp.web.lists.getByTitle('SPS BCN Announcements').items.get().then(function (data) {
        var collection = data.map(function (announcement) {
            return new Announcement(announcement.Title, announcement.SPSBcnDescription);
        });

        console.log(collection);

        self.announcements(collection);

    }).catch(function (err) {
        console.log(err);
    });

};