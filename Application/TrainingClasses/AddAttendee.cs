using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Application.Errors;
using Application.Interfaces;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistance;

namespace Application.TrainingClasses {
    public class AddAttendee {

        public class Command : IRequest {
            [Required]
            public Guid TrainingClassId { get; set; }
        }
        public class Handler : IRequestHandler<Command> {
            private readonly DataContext _context;
            private readonly IUserAccessor _userAccessor;

            public Handler (DataContext context, IUserAccessor userAccessor) {
                _userAccessor = userAccessor;
                _context = context;
            }

            public async Task<Unit> Handle (Command request, CancellationToken cancellationToken) {
                var user = await _context.Users.FirstOrDefaultAsync (x => x.UserName == _userAccessor.GetCurrentUsername ());
                if (user == null)
                    throw new ErrorException (HttpStatusCode.Unauthorized);
                var TrainingClass = await _context.TrainingClasses.Include (x => x.UserTrainingClasses).FirstOrDefaultAsync (x => x.Id == request.TrainingClassId);
                if (TrainingClass == null)
                    throw new ErrorException (HttpStatusCode.NotFound);

                ///Check if attendance exists
                var attendance = await _context.UserTrainingClasses.FirstOrDefaultAsync (x => x.UserId == user.Id && x.TrainingClassId == TrainingClass.Id);
                if (attendance != null)
                    throw new ErrorException (HttpStatusCode.BadRequest, new { AddAttendee = "You already attending this training class" });
                //Spot Filled or not
                if (TrainingClass.TotalSpots < TrainingClass.UserTrainingClasses.Count + 1) {
                    throw new ErrorException (HttpStatusCode.BadRequest, new { AddAttendee = "Sorry the class is full" });
                }
                _context.UserTrainingClasses.Add (new UserTrainingClass {
                    User = user,
                        TrainingClass = TrainingClass,
                        DateJoined = DateTime.Now,
                        IsHost = false
                });
                if (await _context.SaveChangesAsync () > 0)
                    return Unit.Value;
                throw new Exception ("Problem saving changes");

            }
        }
    }
}